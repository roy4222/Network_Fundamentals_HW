using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace P2PMessenger
{
    /// <summary>
    /// 主要表單 - UDP P2P訊息應用程式
    /// 提供基本的發送和接收文字訊息功能，支援暱稱系統
    /// </summary>
    public partial class MainForm : Form
    {
        private UdpSender? udpSender;
        private UdpListener? udpListener;
        private int localPort = 8001;
        
        // UI控制項
        private TextBox txtLocalPort = null!;
        private TextBox txtRemoteIp = null!;
        private TextBox txtRemotePort = null!;
        private TextBox txtNickname = null!;  // 新增：暱稱輸入欄位
        private TextBox txtMessage = null!;
        private TextBox txtReceivedMessages = null!;
        private Button btnStartListening = null!;
        private Button btnStopListening = null!;
        private Button btnSendMessage = null!;
        private Label lblStatus = null!;

        /// <summary>
        /// 初始化主表單
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitializeUdpComponents();
        }

        /// <summary>
        /// 初始化表單控制項
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "UDP P2P 訊息應用程式 (支援暱稱)";
            this.Size = new Size(600, 550);  // 增加高度以容納新控制項
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 建立控制項
            CreateControls();
            SetupLayout();
            SetupEventHandlers();
        }

        /// <summary>
        /// 建立所有UI控制項
        /// </summary>
        private void CreateControls()
        {
            // 本地端口設定
            var lblLocalPort = new Label
            {
                Text = "本地監聽端口:",
                Location = new Point(10, 15),
                Size = new Size(100, 23)
            };
            
            txtLocalPort = new TextBox
            {
                Text = localPort.ToString(),
                Location = new Point(120, 12),
                Size = new Size(80, 23)
            };

            // 遠端連線設定
            var lblRemoteIp = new Label
            {
                Text = "目標IP:",
                Location = new Point(220, 15),
                Size = new Size(60, 23)
            };
            
            txtRemoteIp = new TextBox
            {
                Text = "127.0.0.1",
                Location = new Point(280, 12),
                Size = new Size(100, 23)
            };

            var lblRemotePort = new Label
            {
                Text = "目標端口:",
                Location = new Point(390, 15),
                Size = new Size(70, 23)
            };
            
            txtRemotePort = new TextBox
            {
                Text = "8002",
                Location = new Point(460, 12),
                Size = new Size(80, 23)
            };

            // 暱稱設定 (新增)
            var lblNickname = new Label
            {
                Text = "您的暱稱:",
                Location = new Point(10, 45),
                Size = new Size(80, 23)
            };
            
            txtNickname = new TextBox
            {
                Text = "使用者",  // 預設暱稱
                Location = new Point(100, 42),
                Size = new Size(120, 23),
                PlaceholderText = "請輸入您的暱稱..."
            };

            // 監聽控制按鈕 (調整位置)
            btnStartListening = new Button
            {
                Text = "開始監聽",
                Location = new Point(10, 80),  // 向下移動
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };

            btnStopListening = new Button
            {
                Text = "停止監聽",
                Location = new Point(100, 80),  // 向下移動
                Size = new Size(80, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };

            // 狀態標籤 (調整位置)
            lblStatus = new Label
            {
                Text = "狀態: 未監聽",
                Location = new Point(200, 85),  // 向下移動
                Size = new Size(200, 20),
                ForeColor = Color.Blue
            };

            // 訊息輸入 (調整位置)
            var lblMessage = new Label
            {
                Text = "發送訊息:",
                Location = new Point(10, 130),  // 向下移動
                Size = new Size(80, 23)
            };

            txtMessage = new TextBox
            {
                Location = new Point(10, 155),  // 向下移動
                Size = new Size(450, 23),
                PlaceholderText = "請輸入要發送的訊息..."
            };

            btnSendMessage = new Button
            {
                Text = "發送",
                Location = new Point(470, 155),  // 向下移動
                Size = new Size(60, 23),
                BackColor = Color.LightBlue
            };

            // 接收訊息顯示 (調整位置)
            var lblReceived = new Label
            {
                Text = "接收的訊息:",
                Location = new Point(10, 190),  // 向下移動
                Size = new Size(100, 23)
            };

            txtReceivedMessages = new TextBox
            {
                Location = new Point(10, 215),  // 向下移動
                Size = new Size(560, 280),  // 調整高度
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };

            // 將控制項加入表單
            this.Controls.AddRange(new Control[]
            {
                lblLocalPort, txtLocalPort, lblRemoteIp, txtRemoteIp,
                lblRemotePort, txtRemotePort, lblNickname, txtNickname,  // 新增暱稱控制項
                btnStartListening, btnStopListening,
                lblStatus, lblMessage, txtMessage, btnSendMessage,
                lblReceived, txtReceivedMessages
            });
        }

        /// <summary>
        /// 設定版面配置
        /// </summary>
        private void SetupLayout()
        {
            // 使用TableLayoutPanel可以讓版面更整潔，但這裡先用絕對定位保持簡單
        }

        /// <summary>
        /// 設定事件處理器
        /// </summary>
        private void SetupEventHandlers()
        {
            btnStartListening.Click += BtnStartListening_Click;
            btnStopListening.Click += BtnStopListening_Click;
            btnSendMessage.Click += BtnSendMessage_Click;
            txtMessage.KeyPress += TxtMessage_KeyPress;
            this.FormClosing += MainForm_FormClosing;
        }

        /// <summary>
        /// 初始化UDP組件
        /// </summary>
        private void InitializeUdpComponents()
        {
            udpSender = new UdpSender();
            AppendMessage("[系統] UDP發送器初始化完成");
        }

        /// <summary>
        /// 開始監聽按鈕事件
        /// </summary>
        private async void BtnStartListening_Click(object? sender, EventArgs e)
        {
            try
            {
                if (int.TryParse(txtLocalPort.Text, out int port))
                {
                    localPort = port;
                    udpListener = new UdpListener(localPort);
                    
                    // 註冊事件
                    udpListener.MessageReceived += OnMessageReceived;
                    udpListener.ListeningStatusChanged += OnListeningStatusChanged;
                    
                    bool success = await udpListener.StartListeningAsync();
                    
                    if (success)
                    {
                        btnStartListening.Enabled = false;
                        btnStopListening.Enabled = true;
                        txtLocalPort.Enabled = false;
                        AppendMessage($"[系統] 開始監聽端口 {localPort}");
                    }
                    else
                    {
                        AppendMessage("[系統] 啟動監聽失敗");
                    }
                }
                else
                {
                    MessageBox.Show("請輸入有效的端口號碼", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"[錯誤] 啟動監聽時發生例外: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止監聽按鈕事件
        /// </summary>
        private async void BtnStopListening_Click(object? sender, EventArgs e)
        {
            try
            {
                if (udpListener != null)
                {
                    await udpListener.StopListeningAsync();
                    udpListener.Dispose();
                    udpListener = null;
                }
                
                btnStartListening.Enabled = true;
                btnStopListening.Enabled = false;
                txtLocalPort.Enabled = true;
                AppendMessage("[系統] 停止監聽");
            }
            catch (Exception ex)
            {
                AppendMessage($"[錯誤] 停止監聽時發生例外: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送訊息按鈕事件
        /// </summary>
        private async void BtnSendMessage_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync();
        }

        /// <summary>
        /// 訊息輸入框按鍵事件（Enter發送）
        /// </summary>
        private async void TxtMessage_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        /// <summary>
        /// 發送訊息的主要方法
        /// </summary>
        private async Task SendMessageAsync()
        {
            try
            {
                string message = txtMessage.Text.Trim();
                string nickname = txtNickname.Text.Trim();
                string remoteIp = txtRemoteIp.Text.Trim();
                
                if (string.IsNullOrEmpty(message))
                {
                    MessageBox.Show("請輸入要發送的訊息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrEmpty(nickname))
                {
                    MessageBox.Show("請輸入您的暱稱", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (int.TryParse(txtRemotePort.Text, out int remotePort))
                {
                    if (udpSender != null)
                    {
                        // 組合訊息格式：暱稱: 訊息內容
                        string formattedMessage = $"{nickname}: {message}";
                        bool success = await udpSender.SendMessageAsync(formattedMessage, remoteIp, remotePort);
                        
                        if (success)
                        {
                            AppendMessage($"[發送] -> {remoteIp}:{remotePort} | {nickname}說：{message}");
                            txtMessage.Clear();
                        }
                        else
                        {
                            AppendMessage("[錯誤] 訊息發送失敗");
                        }
                    }
                    else
                    {
                        AppendMessage("[錯誤] UDP發送器未初始化");
                    }
                }
                else
                {
                    MessageBox.Show("請輸入有效的目標端口號碼", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"[錯誤] 發送訊息時發生例外: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理接收到的訊息
        /// </summary>
        private void OnMessageReceived(string message, string senderIp, int senderPort)
        {
            // 使用Invoke確保在UI線程中更新界面
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnMessageReceived(message, senderIp, senderPort)));
                return;
            }

            // 解析訊息格式：暱稱: 訊息內容
            string displayMessage;
            if (message.Contains(": "))
            {
                // 找到第一個 ": " 的位置
                int separatorIndex = message.IndexOf(": ");
                string nickname = message.Substring(0, separatorIndex);
                string content = message.Substring(separatorIndex + 2);
                
                // 格式化顯示：[時間] 暱稱說：訊息內容
                displayMessage = $"{nickname}說：{content}";
            }
            else
            {
                // 如果沒有暱稱格式，直接顯示原始訊息
                displayMessage = $"匿名用戶說：{message}";
            }

            AppendMessage($"[接收] <- {senderIp}:{senderPort} | {displayMessage}");
        }

        /// <summary>
        /// 處理監聽狀態變更
        /// </summary>
        private void OnListeningStatusChanged(bool isListening)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnListeningStatusChanged(isListening)));
                return;
            }

            lblStatus.Text = isListening ? $"狀態: 正在監聽端口 {localPort}" : "狀態: 未監聽";
            lblStatus.ForeColor = isListening ? Color.Green : Color.Blue;
        }

        /// <summary>
        /// 在訊息顯示區域加入新訊息
        /// </summary>
        private void AppendMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AppendMessage(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtReceivedMessages.AppendText($"[{timestamp}] {message}\r\n");
            txtReceivedMessages.ScrollToCaret();
        }

        /// <summary>
        /// 表單關閉事件
        /// </summary>
        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (udpListener != null)
                {
                    await udpListener.StopListeningAsync();
                    udpListener.Dispose();
                }

                udpSender?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"關閉表單時發生錯誤: {ex.Message}");
            }
        }
    }
} 