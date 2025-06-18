using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace P2PMessenger
{
    /// <summary>
    /// P2P 訊息傳遞系統主視窗
    /// 實現 Windows Form 座標系統 (左上角為原點 0,0)
    /// 包含繪圖區域和網路設定介面
    /// </summary>
    public partial class MainForm : Form
    {
        // 網路通訊元件
        private P2PNetworkManager networkManager = null!;
        
        // UI 控制項
        private Panel drawingPanel = null!;
        private GroupBox networkSettingsGroup = null!;
        private TextBox localIpTextBox = null!;
        private TextBox remoteIpTextBox = null!;
        private TextBox portTextBox = null!;
        private Button connectButton = null!;
        private Button disconnectButton = null!;
        private Label statusLabel = null!;
        private Label coordinateLabel = null!;
        
        // 繪圖相關
        private bool isDrawing = false;
        private Point lastPoint;
        private Graphics drawingGraphics = null!;

        /// <summary>
        /// 初始化主視窗
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitializeNetworkManager();
            
            Console.WriteLine("主視窗初始化完成");
        }

        /// <summary>
        /// 初始化 UI 元件
        /// 遵循 Windows Form 座標系統 (0,0) 在左上角
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // 主視窗設定
            this.Text = "P2P 訊息傳遞系統 - EXP2";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // 建立網路設定群組
            CreateNetworkSettingsGroup();
            
            // 建立繪圖區域
            CreateDrawingPanel();
            
            // 建立狀態列
            CreateStatusControls();
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 建立網路設定群組
        /// 位置：左上角區域 (x=10, y=10)
        /// </summary>
        private void CreateNetworkSettingsGroup()
        {
            networkSettingsGroup = new GroupBox();
            networkSettingsGroup.Text = "網路設定";
            networkSettingsGroup.Location = new Point(10, 10);  // 左上角座標
            networkSettingsGroup.Size = new Size(300, 120);
            
            // 本機 IP 設定
            Label localIpLabel = new Label();
            localIpLabel.Text = "本機 IP:";
            localIpLabel.Location = new Point(10, 25);
            localIpLabel.Size = new Size(60, 20);
            
            localIpTextBox = new TextBox();
            localIpTextBox.Location = new Point(80, 23);
            localIpTextBox.Size = new Size(120, 20);
            localIpTextBox.Text = GetLocalIPAddress();
            
            // 遠端 IP 設定
            Label remoteIpLabel = new Label();
            remoteIpLabel.Text = "遠端 IP:";
            remoteIpLabel.Location = new Point(10, 50);
            remoteIpLabel.Size = new Size(60, 20);
            
            remoteIpTextBox = new TextBox();
            remoteIpTextBox.Location = new Point(80, 48);
            remoteIpTextBox.Size = new Size(120, 20);
            remoteIpTextBox.Text = "192.168.0.200";
            
            // 埠號設定
            Label portLabel = new Label();
            portLabel.Text = "埠號:";
            portLabel.Location = new Point(10, 75);
            portLabel.Size = new Size(60, 20);
            
            portTextBox = new TextBox();
            portTextBox.Location = new Point(80, 73);
            portTextBox.Size = new Size(120, 20);
            portTextBox.Text = "8888";
            
            // 連線按鈕
            connectButton = new Button();
            connectButton.Text = "連線";
            connectButton.Location = new Point(210, 23);
            connectButton.Size = new Size(75, 25);
            connectButton.Click += ConnectButton_Click;
            
            // 斷線按鈕
            disconnectButton = new Button();
            disconnectButton.Text = "斷線";
            disconnectButton.Location = new Point(210, 53);
            disconnectButton.Size = new Size(75, 25);
            disconnectButton.Enabled = false;
            disconnectButton.Click += DisconnectButton_Click;
            
            // 將控制項加入群組
            networkSettingsGroup.Controls.AddRange(new Control[] {
                localIpLabel, localIpTextBox,
                remoteIpLabel, remoteIpTextBox,
                portLabel, portTextBox,
                connectButton, disconnectButton
            });
            
            this.Controls.Add(networkSettingsGroup);
        }

        /// <summary>
        /// 建立繪圖區域
        /// 位置：網路設定群組下方
        /// </summary>
        private void CreateDrawingPanel()
        {
            drawingPanel = new Panel();
            drawingPanel.Location = new Point(10, 140);  // 網路設定群組下方
            drawingPanel.Size = new Size(970, 480);
            drawingPanel.BackColor = Color.White;
            drawingPanel.BorderStyle = BorderStyle.FixedSingle;
            
            // 繪圖事件處理
            drawingPanel.MouseDown += DrawingPanel_MouseDown;
            drawingPanel.MouseMove += DrawingPanel_MouseMove;
            drawingPanel.MouseUp += DrawingPanel_MouseUp;
            drawingPanel.Paint += DrawingPanel_Paint;
            
            this.Controls.Add(drawingPanel);
            
            // 初始化繪圖物件
            drawingGraphics = drawingPanel.CreateGraphics();
        }

        /// <summary>
        /// 建立狀態控制項
        /// 位置：視窗底部
        /// </summary>
        private void CreateStatusControls()
        {
            statusLabel = new Label();
            statusLabel.Text = "狀態: 未連線";
            statusLabel.Location = new Point(10, 630);
            statusLabel.Size = new Size(200, 20);
            statusLabel.ForeColor = Color.Red;
            
            coordinateLabel = new Label();
            coordinateLabel.Text = "座標: (0, 0)";
            coordinateLabel.Location = new Point(220, 630);
            coordinateLabel.Size = new Size(150, 20);
            
            this.Controls.AddRange(new Control[] { statusLabel, coordinateLabel });
        }

        /// <summary>
        /// 初始化網路管理器
        /// </summary>
        private void InitializeNetworkManager()
        {
            networkManager = new P2PNetworkManager();
            networkManager.DataReceived += NetworkManager_DataReceived;
            networkManager.ConnectionStatusChanged += NetworkManager_ConnectionStatusChanged;
        }

        /// <summary>
        /// 取得本機 IP 位址
        /// </summary>
        /// <returns>本機 IP 位址字串</returns>
        private string GetLocalIPAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
                
                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得本機 IP 失敗: {ex.Message}");
            }
            
            return "127.0.0.1";
        }

        #region 網路事件處理

        /// <summary>
        /// 連線按鈕點擊事件
        /// </summary>
        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            try
            {
                string localIp = localIpTextBox.Text.Trim();
                string remoteIp = remoteIpTextBox.Text.Trim();
                int port = int.Parse(portTextBox.Text.Trim());
                
                Console.WriteLine($"嘗試連線: 本機={localIp}, 遠端={remoteIp}, 埠號={port}");
                
                networkManager.StartConnection(localIp, remoteIp, port);
                
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"連線失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"連線錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 斷線按鈕點擊事件
        /// </summary>
        private void DisconnectButton_Click(object? sender, EventArgs e)
        {
            networkManager.StopConnection();
            
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            
            Console.WriteLine("手動斷線");
        }

        /// <summary>
        /// 網路資料接收事件
        /// </summary>
        private void NetworkManager_DataReceived(object? sender, StrokeDataEventArgs e)
        {
            // 在 UI 執行緒中處理接收到的筆跡資料
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DrawReceivedStroke(e.StrokeData)));
            }
            else
            {
                DrawReceivedStroke(e.StrokeData);
            }
        }

        /// <summary>
        /// 連線狀態變更事件
        /// </summary>
        private void NetworkManager_ConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateConnectionStatus(e.IsConnected, e.Message)));
            }
            else
            {
                UpdateConnectionStatus(e.IsConnected, e.Message);
            }
        }

        /// <summary>
        /// 更新連線狀態顯示
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected, string message)
        {
            statusLabel.Text = $"狀態: {message}";
            statusLabel.ForeColor = isConnected ? Color.Green : Color.Red;
            
            Console.WriteLine($"連線狀態更新: {message}");
        }

        #endregion

        #region 繪圖事件處理

        /// <summary>
        /// 滑鼠按下事件 - 開始繪圖
        /// </summary>
        private void DrawingPanel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = true;
                lastPoint = e.Location;
                
                // 更新座標顯示
                coordinateLabel.Text = $"座標: ({e.X}, {e.Y})";
                
                Console.WriteLine($"開始繪圖於座標: ({e.X}, {e.Y})");
            }
        }

        /// <summary>
        /// 滑鼠移動事件 - 繪製線條
        /// </summary>
        private void DrawingPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            coordinateLabel.Text = $"座標: ({e.X}, {e.Y})";
            
            if (isDrawing && e.Button == MouseButtons.Left)
            {
                // 在本地繪製
                using (Pen pen = new Pen(Color.Black, 2))
                {
                    drawingGraphics.DrawLine(pen, lastPoint, e.Location);
                }
                
                // 建立筆跡資料並傳送
                StrokeData strokeData = new StrokeData
                {
                    StartPoint = lastPoint,
                    EndPoint = e.Location,
                    Timestamp = DateTime.Now,
                    Color = Color.Black,
                    Width = 2
                };
                
                // 透過網路傳送筆跡資料
                networkManager.SendStrokeData(strokeData);
                
                lastPoint = e.Location;
            }
        }

        /// <summary>
        /// 滑鼠放開事件 - 結束繪圖
        /// </summary>
        private void DrawingPanel_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = false;
                Console.WriteLine($"結束繪圖於座標: ({e.X}, {e.Y})");
            }
        }

        /// <summary>
        /// 繪圖區域重繪事件
        /// </summary>
        private void DrawingPanel_Paint(object? sender, PaintEventArgs e)
        {
            // 這裡可以重繪所有筆跡（如果需要持久化）
        }

        /// <summary>
        /// 繪製接收到的筆跡
        /// </summary>
        private void DrawReceivedStroke(StrokeData strokeData)
        {
            using (Pen pen = new Pen(strokeData.Color, strokeData.Width))
            {
                drawingGraphics.DrawLine(pen, strokeData.StartPoint, strokeData.EndPoint);
            }
            
            Console.WriteLine($"繪製接收筆跡: ({strokeData.StartPoint.X},{strokeData.StartPoint.Y}) -> ({strokeData.EndPoint.X},{strokeData.EndPoint.Y})");
        }

        #endregion

        ///<summary>
        /// 視窗關閉時清理資源
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            networkManager?.StopConnection();
            drawingGraphics?.Dispose();
            
            Console.WriteLine("應用程式關閉，資源已清理");
            base.OnFormClosed(e);
        }
    }
} 