using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Concurrent;

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
        private readonly ConcurrentDictionary<string, bool> _unreadMessages = new ConcurrentDictionary<string, bool>();
        private string[] _currentUserList = Array.Empty<string>();

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
            _privateMessageButton.Click += (s, e) => SendPrivateMessage();

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

            // 使用者列表雙擊私訊 -> 改為 @ 功能
            _userList.DoubleClick += (s, e) =>
            {
                if (_userList.SelectedItem is string selectedItem)
                {
                    // 解析出實際的使用者名稱 (移除星號)
                    string targetUsername = selectedItem.Split(' ')[0];

                    if (!string.IsNullOrEmpty(targetUsername))
                    {
                        // 標記為已讀並刷新列表
                        if (_unreadMessages.TryRemove(targetUsername, out _))
                        {
                            RefreshUserListDisplay();
                        }

                        // 在訊息框中插入 @使用者名稱
                        _messageInput.Text = $"@{targetUsername} ";
                        _messageInput.Focus();
                        _messageInput.Select(_messageInput.Text.Length, 0); // 將游標移到最後
                    }
                }
            };

            // TCP 客戶端事件
            _tcpClient.MessageReceived += OnMessageReceived;
            _tcpClient.PrivateMessageReceived += OnPrivateMessageReceived;
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
        /// 發送訊息 (廣播或私訊)
        /// </summary>
        private async Task SendMessage()
        {
            string message = _messageInput.Text.Trim();
            if (string.IsNullOrEmpty(message) || !_isConnected)
            {
                return;
            }

            // 檢查是否為私訊 (以 @username 開頭)
            if (message.StartsWith("@"))
            {
                string[] parts = message.Split(new[] { ' ' }, 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    string targetUsername = parts[0].Substring(1); // 移除 @
                    string privateMessage = parts[1];

                    // 不能私訊自己
                    if (targetUsername.Equals(_tcpClient.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("不能私訊自己。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    
                    await _tcpClient.SendPrivateMessageAsync(targetUsername, privateMessage);
                    _messageInput.Clear();
                }
                else
                {
                    // 格式不正確的提示
                    MessageBox.Show("私訊格式不正確，請使用 @使用者名稱 訊息 的格式。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // 廣播訊息
                await _tcpClient.SendBroadcastMessageAsync(message);
                _messageInput.Clear();
            }
        }

        /// <summary>
        /// 發送私人訊息 (此方法已由 SendMessage 中的 @ 功能取代)
        /// </summary>
        private void SendPrivateMessage()
        {
            if (_userList.SelectedItem == null)
            {
                MessageBox.Show("請先選擇要私訊的使用者", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetUsername = _userList.SelectedItem.ToString()!;
            if (targetUsername.Equals(_tcpClient.Username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("不能私訊自己。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // 這裡可以保留舊的彈窗邏輯作為備用，或完全移除
            // 為了簡化，我們暫時讓它不做任何事，因為新邏輯在 SendMessage 中
            MessageBox.Show("私訊功能已整合至主輸入框，請雙擊使用者列表中的名稱，然後在輸入框中以 @使用者 格式發送。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void OnPrivateMessageReceived(string sender, string message)
        {
            AppendMessage(message);

            // 如果不是自己發的訊息，就標記為未讀
            if (sender != _tcpClient.Username)
            {
                _unreadMessages[sender] = true;
                RefreshUserListDisplay();
            }
        }

        /// <summary>
        /// 處理使用者列表更新
        /// </summary>
        private void OnUserListUpdated(string[] users)
        {
            _currentUserList = users;
            RefreshUserListDisplay();
        }

        private void RefreshUserListDisplay()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshUserListDisplay));
                return;
            }

            _userList.BeginUpdate();
            _userList.Items.Clear();
            foreach (var user in _currentUserList)
            {
                if (_unreadMessages.ContainsKey(user))
                {
                    _userList.Items.Add($"{user} (*)");
                }
                else
                {
                    _userList.Items.Add(user);
                }
            }
            _userList.EndUpdate();

            _userCountLabel.Text = $"線上人數: {_currentUserList.Length}";
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