using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient
{
    /// <summary>
    /// TCP 聊天客戶端核心類別
    /// 負責與伺服器連線、發送訊息和接收回應
    /// </summary>
    public class TcpClient
    {
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private System.Net.Sockets.TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected;

        public string Username { get; private set; } = string.Empty;
        public bool IsConnected => _isConnected && _client?.Connected == true;

        // 事件處理
        public event Action<string>? MessageReceived;
        public event Action<string[]>? UserListUpdated;
        public event Action<string, string>? PrivateMessageReceived;
        public event Action<string>? ErrorOccurred;
        public event Action? Disconnected;

        public TcpClient(string serverAddress, int serverPort)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            Console.WriteLine($"[客戶端] 初始化完成，目標伺服器: {_serverAddress}:{_serverPort}");
        }

        /// <summary>
        /// 連線到伺服器
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Console.WriteLine($"[客戶端] 正在連線到 {_serverAddress}:{_serverPort}...");
                
                _client = new System.Net.Sockets.TcpClient();
                await _client.ConnectAsync(_serverAddress, _serverPort);
                
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                
                _isConnected = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                Console.WriteLine("[客戶端] 連線成功");
                
                // 開始監聽伺服器訊息
                _ = Task.Run(() => ListenForMessagesAsync(_cancellationTokenSource.Token));
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 連線失敗: {ex.Message}");
                ErrorOccurred?.Invoke($"連線失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 登入到伺服器
        /// </summary>
        public async Task<bool> LoginAsync(string username)
        {
            if (!IsConnected)
            {
                ErrorOccurred?.Invoke("尚未連線到伺服器");
                return false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorOccurred?.Invoke("使用者名稱不能為空");
                return false;
            }

            try
            {
                string loginMessage = MessageProtocol.CreateLoginMessage(username);
                await SendMessageAsync(loginMessage);
                
                Username = username;
                Console.WriteLine($"[客戶端] 發送登入請求: {username}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 登入失敗: {ex.Message}");
                ErrorOccurred?.Invoke($"登入失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 發送廣播訊息
        /// </summary>
        public async Task SendBroadcastMessageAsync(string message)
        {
            if (!IsConnected)
            {
                ErrorOccurred?.Invoke("尚未連線到伺服器");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                ErrorOccurred?.Invoke("訊息內容不能為空");
                return;
            }

            try
            {
                string broadcastMessage = MessageProtocol.CreateBroadcastMessage(message);
                await SendMessageAsync(broadcastMessage);
                Console.WriteLine($"[客戶端] 發送廣播訊息: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 發送廣播訊息失敗: {ex.Message}");
                ErrorOccurred?.Invoke($"發送訊息失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送私人訊息
        /// </summary>
        public async Task SendPrivateMessageAsync(string targetUsername, string message)
        {
            if (!IsConnected)
            {
                ErrorOccurred?.Invoke("尚未連線到伺服器");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetUsername) || string.IsNullOrWhiteSpace(message))
            {
                ErrorOccurred?.Invoke("目標使用者或訊息內容不能為空");
                return;
            }

            try
            {
                string privateMessage = MessageProtocol.CreatePrivateMessage(targetUsername, message);
                await SendMessageAsync(privateMessage);
                Console.WriteLine($"[客戶端] 發送私訊給 {targetUsername}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 發送私訊失敗: {ex.Message}");
                ErrorOccurred?.Invoke($"發送私訊失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 登出並斷線
        /// </summary>
        public async Task LogoutAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(Username))
            {
                await DisconnectAsync();
                return;
            }

            try
            {
                string logoutMessage = MessageProtocol.CreateLogoutMessage(Username);
                await SendMessageAsync(logoutMessage);
                Console.WriteLine($"[客戶端] 發送登出請求: {Username}");
                
                await Task.Delay(1000); // 等待伺服器處理
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 登出時發生錯誤: {ex.Message}");
            }
            finally
            {
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// 斷線
        /// </summary>
        public Task DisconnectAsync()
        {
            try
            {
                Console.WriteLine("[客戶端] 正在斷線...");
                _isConnected = false;
                
                _cancellationTokenSource?.Cancel();
                
                _reader?.Close();
                _writer?.Close();
                _stream?.Close();
                _client?.Close();
                
                Username = string.Empty;
                
                Console.WriteLine("[客戶端] 已斷線");
                Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 斷線時發生錯誤: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 發送原始訊息到伺服器
        /// </summary>
        private async Task SendMessageAsync(string message)
        {
            try
            {
                if (!IsConnected)
                {
                    Console.WriteLine("[客戶端] 無法發送訊息 - 未連線到伺服器");
                    throw new InvalidOperationException("未連線到伺服器");
                }

                if (_writer == null)
                {
                    Console.WriteLine("[客戶端] 無法發送訊息 - StreamWriter 為空");
                    throw new InvalidOperationException("StreamWriter 未初始化");
                }

                Console.WriteLine($"[客戶端] 正在發送訊息: {message}");
                await _writer.WriteLineAsync(message);
                await _writer.FlushAsync();
                Console.WriteLine($"[客戶端] 訊息發送成功: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 發送訊息失敗: {ex.Message}");
                throw; // 重新拋出異常以便上層處理
            }
        }

        /// <summary>
        /// 監聽伺服器訊息的主迴圈
        /// </summary>
        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[客戶端] 開始監聽伺服器訊息");
                
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var line = await _reader.ReadLineAsync(cancellationToken);
                        if (line == null)
                        {
                            Console.WriteLine("[客戶端] 伺服器已關閉連線");
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            Console.WriteLine($"[客戶端] 收到伺服器訊息: {line}");
                            ProcessServerMessageAsync(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[客戶端] 讀取伺服器訊息時發生錯誤: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 監聽訊息時發生錯誤: {ex.Message}");
            }
            finally
            {
                if (IsConnected)
                {
                    await DisconnectAsync();
                }
            }
        }

        /// <summary>
        /// 處理從伺服器收到的單一訊息
        /// </summary>
        private void ProcessServerMessageAsync(string message)
        {
            try
            {
                var (messageType, content) = MessageProtocol.ParseMessage(message);
                string timestamp = DateTime.Now.ToString("HH:mm:ss");

                switch (messageType)
                {
                    case MessageProtocol.BROADCAST:
                        // 廣播格式為 "username:message"
                        var broadcastParts = content[0].Split(new[] { ':' }, 2);
                        if (broadcastParts.Length == 2)
                        {
                            MessageReceived?.Invoke(MessageProtocol.FormatBroadcastDisplay(timestamp, broadcastParts[0], broadcastParts[1]));
                        }
                        else
                        {
                            MessageReceived?.Invoke(content[0]); // Fallback
                        }
                        break;

                    case MessageProtocol.SUCCESS:
                    case MessageProtocol.SYSTEM_NOTIFICATION:
                        MessageReceived?.Invoke(MessageProtocol.FormatSystemNotificationDisplay(timestamp, content[0]));
                        break;

                    case MessageProtocol.USER_JOINED:
                        MessageReceived?.Invoke(MessageProtocol.FormatUserJoinedDisplay(timestamp, content[0]));
                        break;

                    case MessageProtocol.USER_LEFT:
                        MessageReceived?.Invoke(MessageProtocol.FormatUserLeftDisplay(timestamp, content[0]));
                        break;
                    
                    case MessageProtocol.PRIVATE:
                        // 私訊格式：senderUsername:formattedMessage
                        var privateParts = content[0].Split(new[] { ':' }, 2);
                        if (privateParts.Length == 2)
                        {
                            PrivateMessageReceived?.Invoke(privateParts[0], privateParts[1]);
                        }
                        else
                        {
                            // 處理格式不符的私訊
                            MessageReceived?.Invoke(content[0]);
                        }
                        break;

                    case MessageProtocol.USER_LIST:
                        var users = content[0].Split(',');
                        UserListUpdated?.Invoke(users);
                        break;

                    case MessageProtocol.ERROR:
                        ErrorOccurred?.Invoke(content[0]);
                        break;

                    case "LOGIN_SUCCESS": // 這個伺服器沒在用，但先留著
                        MessageReceived?.Invoke("✓ 登入成功");
                        break;
                        
                    default:
                        Console.WriteLine($"[客戶端] 收到未處理的訊息類型: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[客戶端] 處理伺服器訊息時發生錯誤: {ex.Message}");
                ErrorOccurred?.Invoke($"處理訊息時發生錯誤: {ex.Message}");
            }
        }
    }
} 