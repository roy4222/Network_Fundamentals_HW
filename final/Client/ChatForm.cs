using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    /// <summary>
    /// 聊天室主介面
    /// 提供群聊、私訊和使用者列表功能
    /// </summary>
    public partial class ChatForm : Form
    {
        private readonly TcpClient _tcpClient;
        private bool _isConnected;

        // UI 控制項
        private TextBox _chatDisplay = null!;
        private TextBox _messageInput = null!;
        private Button _sendButton = null!;
        private ListBox _userList = null!;
        private Label _statusLabel = null!;
        private Label _userCountLabel = null!;
        private Button _connectButton = null!;
        private Button _disconnectButton = null!;
        private TextBox _usernameInput = null!;
        private Button _privateMessageButton = null!;

        public ChatForm()
        {
            _tcpClient = new TcpClient("127.0.0.1", 8888);
            InitializeComponent();
            SetupEventHandlers();
        }

        /// <summary>
        /// 初始化 UI 控制項
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 表單設定
            this.Text = "多人線上聊天室 - 里程碑 2";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 使用者名稱輸入區域
            var usernameLabel = new Label
            {
                Text = "使用者名稱:",
                Location = new Point(10, 10),
                Size = new Size(80, 23)
            };
            this.Controls.Add(usernameLabel);

            _usernameInput = new TextBox
            {
                Location = new Point(100, 10),
                Size = new Size(150, 23),
                Text = $"User{new Random().Next(1000, 9999)}" // 預設隨機使用者名稱
            };
            this.Controls.Add(_usernameInput);

            // 連線控制按鈕
            _connectButton = new Button
            {
                Text = "連線",
                Location = new Point(260, 10),
                Size = new Size(70, 25),
                BackColor = Color.LightGreen
            };
            this.Controls.Add(_connectButton);

            _disconnectButton = new Button
            {
                Text = "斷線",
                Location = new Point(340, 10),
                Size = new Size(70, 25),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            this.Controls.Add(_disconnectButton);

            // 狀態標籤
            _statusLabel = new Label
            {
                Text = "狀態: 未連線",
                Location = new Point(420, 15),
                Size = new Size(200, 20),
                ForeColor = Color.Red
            };
            this.Controls.Add(_statusLabel);

            // 聊天顯示區域
            _chatDisplay = new TextBox
            {
                Location = new Point(10, 50),
                Size = new Size(550, 400),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("微軟正黑體", 9F)
            };
            this.Controls.Add(_chatDisplay);

            // 使用者列表
            var userListLabel = new Label
            {
                Text = "線上使用者:",
                Location = new Point(570, 50),
                Size = new Size(100, 20)
            };
            this.Controls.Add(userListLabel);

            _userList = new ListBox
            {
                Location = new Point(570, 75),
                Size = new Size(200, 300),
                Font = new Font("微軟正黑體", 9F)
            };
            this.Controls.Add(_userList);

            _userCountLabel = new Label
            {
                Text = "線上人數: 0",
                Location = new Point(570, 380),
                Size = new Size(100, 20),
                ForeColor = Color.Blue
            };
            this.Controls.Add(_userCountLabel);

            // 私訊按鈕
            _privateMessageButton = new Button
            {
                Text = "發送私訊",
                Location = new Point(570, 405),
                Size = new Size(90, 25),
                BackColor = Color.LightBlue,
                Enabled = false
            };
            this.Controls.Add(_privateMessageButton);

            // 訊息輸入區域
            _messageInput = new TextBox
            {
                Location = new Point(10, 460),
                Size = new Size(450, 23),
                Enabled = false
            };
            this.Controls.Add(_messageInput);

            _sendButton = new Button
            {
                Text = "發送",
                Location = new Point(470, 460),
                Size = new Size(90, 25),
                BackColor = Color.LightYellow,
                Enabled = false
            };
            this.Controls.Add(_sendButton);

            // 訊息輸入提示
            var inputLabel = new Label
            {
                Text = "訊息輸入區域 (Enter 發送):",
                Location = new Point(10, 490),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };
            this.Controls.Add(inputLabel);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 設定事件處理器
        /// </summary>
        private void SetupEventHandlers()
        {
            // 按鈕事件
            _connectButton.Click += async (s, e) => await ConnectToServer();
            _disconnectButton.Click += async (s, e) => await DisconnectFromServer();
            _sendButton.Click += async (s, e) => await SendMessage();
            _privateMessageButton.Click += async (s, e) => await SendPrivateMessage();

            // 輸入框 Enter 鍵發送
            _messageInput.KeyPress += async (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    await SendMessage();
                }
            };

            // 使用者名稱輸入框 Enter 鍵連線
            _usernameInput.KeyPress += async (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter && _connectButton.Enabled)
                {
                    e.Handled = true;
                    await ConnectToServer();
                }
            };

            // 使用者列表雙擊私訊
            _userList.DoubleClick += async (s, e) => await SendPrivateMessage();

            // TCP 客戶端事件
            _tcpClient.MessageReceived += OnMessageReceived;
            _tcpClient.UserListUpdated += OnUserListUpdated;
            _tcpClient.ErrorOccurred += OnErrorOccurred;
            _tcpClient.Disconnected += OnDisconnected;

            // 表單關閉事件
            this.FormClosing += async (s, e) =>
            {
                if (_isConnected)
                {
                    await _tcpClient.LogoutAsync();
                }
            };
        }

        /// <summary>
        /// 連線到伺服器
        /// </summary>
        private async Task ConnectToServer()
        {
            if (string.IsNullOrWhiteSpace(_usernameInput.Text))
            {
                MessageBox.Show("請輸入使用者名稱", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _connectButton.Enabled = false;
                _usernameInput.Enabled = false;
                UpdateStatus("正在連線...", Color.Orange);

                bool connected = await _tcpClient.ConnectAsync();
                if (connected)
                {
                    bool loggedIn = await _tcpClient.LoginAsync(_usernameInput.Text);
                    if (loggedIn)
                    {
                        _isConnected = true;
                        _disconnectButton.Enabled = true;
                        _messageInput.Enabled = true;
                        _sendButton.Enabled = true;
                        _privateMessageButton.Enabled = true;
                        
                        UpdateStatus($"已連線 - {_tcpClient.Username}", Color.Green);
                        AppendMessage($"✓ 成功連線到聊天室，歡迎 {_tcpClient.Username}！");
                        
                        _messageInput.Focus();
                    }
                    else
                    {
                        await _tcpClient.DisconnectAsync();
                        ResetConnectionState();
                    }
                }
                else
                {
                    ResetConnectionState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"連線失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetConnectionState();
            }
        }

        /// <summary>
        /// 從伺服器斷線
        /// </summary>
        private async Task DisconnectFromServer()
        {
            try
            {
                UpdateStatus("正在斷線...", Color.Orange);
                await _tcpClient.LogoutAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"斷線時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                ResetConnectionState();
            }
        }

        /// <summary>
        /// 發送群聊訊息
        /// </summary>
        private async Task SendMessage()
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(_messageInput.Text))
                return;

            try
            {
                string message = _messageInput.Text.Trim();
                await _tcpClient.SendBroadcastMessageAsync(message);
                _messageInput.Clear();
                _messageInput.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"發送訊息失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 發送私人訊息
        /// </summary>
        private async Task SendPrivateMessage()
        {
            if (!_isConnected || _userList.SelectedItem == null)
            {
                MessageBox.Show("請選擇要私訊的使用者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetUser = _userList.SelectedItem.ToString()!;
            if (targetUser == _tcpClient.Username)
            {
                MessageBox.Show("不能私訊自己", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? message = PrivateMessageDialog.ShowDialog(targetUser);

            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    await _tcpClient.SendPrivateMessageAsync(targetUser, message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"發送私訊失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 重置連線狀態
        /// </summary>
        private void ResetConnectionState()
        {
            _isConnected = false;
            _connectButton.Enabled = true;
            _disconnectButton.Enabled = false;
            _usernameInput.Enabled = true;
            _messageInput.Enabled = false;
            _sendButton.Enabled = false;
            _privateMessageButton.Enabled = false;
            
            UpdateStatus("未連線", Color.Red);
            _userList.Items.Clear();
            _userCountLabel.Text = "線上人數: 0";
        }

        /// <summary>
        /// 更新狀態顯示
        /// </summary>
        private void UpdateStatus(string status, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status, color)));
                return;
            }

            _statusLabel.Text = $"狀態: {status}";
            _statusLabel.ForeColor = color;
        }

        /// <summary>
        /// 追加訊息到聊天顯示區
        /// </summary>
        private void AppendMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AppendMessage(message)));
                return;
            }

            _chatDisplay.AppendText(message + Environment.NewLine);
            _chatDisplay.ScrollToCaret();
        }

        /// <summary>
        /// 處理收到的訊息
        /// </summary>
        private void OnMessageReceived(string message)
        {
            AppendMessage(message);
        }

        /// <summary>
        /// 處理使用者列表更新
        /// </summary>
        private void OnUserListUpdated(string[] users)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnUserListUpdated(users)));
                return;
            }

            _userList.Items.Clear();
            foreach (string user in users.OrderBy(u => u))
            {
                _userList.Items.Add(user);
            }
            _userCountLabel.Text = $"線上人數: {users.Length}";
        }

        /// <summary>
        /// 處理錯誤事件
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnErrorOccurred(error)));
                return;
            }

            AppendMessage($"❌ 錯誤: {error}");
        }

        /// <summary>
        /// 處理斷線事件
        /// </summary>
        private void OnDisconnected()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnDisconnected()));
                return;
            }

            ResetConnectionState();
            AppendMessage("❌ 與伺服器的連線已中斷");
        }
    }
} 