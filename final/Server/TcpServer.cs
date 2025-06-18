using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    /// <summary>
    /// TCP 聊天伺服器核心類別
    /// 負責處理多客戶端連線、訊息路由和使用者會話管理
    /// </summary>
    public class TcpServer
    {
        private readonly int _port;
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;

        // 使用執行緒安全的集合管理客戶端連線
        private readonly ConcurrentDictionary<string, ClientConnection> _clients;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 客戶端連線資訊
        /// </summary>
        private class ClientConnection
        {
            public string Username { get; set; } = string.Empty;
            public TcpClient TcpClient { get; set; }
            public NetworkStream Stream { get; set; }
            public DateTime ConnectedTime { get; set; }

            public ClientConnection(TcpClient tcpClient, NetworkStream stream)
            {
                TcpClient = tcpClient;
                Stream = stream;
                ConnectedTime = DateTime.Now;
            }
        }

        public TcpServer(int port = 8888)
        {
            _port = port;
            _clients = new ConcurrentDictionary<string, ClientConnection>();
            Console.WriteLine($"[伺服器] 初始化完成，監聽埠: {_port}");
        }

        /// <summary>
        /// 啟動伺服器
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                Console.WriteLine($"[伺服器] 啟動成功，監聽埠 {_port}");
                Console.WriteLine("[伺服器] 等待客戶端連線...");

                // 開始接受客戶端連線
                await AcceptClientsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[伺服器] 啟動失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止伺服器
        /// </summary>
        public void Stop()
        {
            try
            {
                Console.WriteLine("[伺服器] 正在停止...");
                _isRunning = false;
                _cancellationTokenSource?.Cancel();

                // 關閉所有客戶端連線
                foreach (var client in _clients.Values)
                {
                    client.Stream?.Close();
                    client.TcpClient?.Close();
                }
                _clients.Clear();

                _listener?.Stop();
                Console.WriteLine("[伺服器] 已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[伺服器] 停止時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 接受客戶端連線的主迴圈
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener!.AcceptTcpClientAsync();
                    Console.WriteLine($"[伺服器] 新客戶端連線: {tcpClient.Client.RemoteEndPoint}");

                    // 為每個客戶端創建獨立的處理工作
                    _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // 伺服器正在關閉
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[伺服器] 接受連線時發生錯誤: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理個別客戶端連線
        /// </summary>
        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            NetworkStream? stream = null;
            ClientConnection? clientConnection = null;
            string clientId = tcpClient.Client.RemoteEndPoint?.ToString() ?? "未知";
            
            try
            {
                stream = tcpClient.GetStream();
                clientConnection = new ClientConnection(tcpClient, stream);
                
                // 使用 StreamReader 進行基於行的讀取
                var reader = new StreamReader(stream, Encoding.UTF8);
                
                Console.WriteLine($"[伺服器] 開始處理客戶端: {clientId}");

                // 讀取客戶端訊息的主迴圈
                while (_isRunning && tcpClient.Connected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        string? message = await reader.ReadLineAsync();
                        
                        if (string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"[伺服器] 客戶端 {clientId} 斷線");
                            break;
                        }

                        message = message.Trim();
                        if (string.IsNullOrEmpty(message))
                        {
                            // 忽略空行
                            continue;
                        }

                        Console.WriteLine($"[伺服器] 收到來自 {clientId} 的訊息: {message}");

                        // 處理收到的訊息
                        await ProcessMessageAsync(clientConnection, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[伺服器] 處理客戶端 {clientId} 訊息時發生錯誤: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[伺服器] 客戶端 {clientId} 連線處理發生錯誤: {ex.Message}");
            }
            finally
            {
                // 清理客戶端連線
                if (!string.IsNullOrEmpty(clientConnection?.Username))
                {
                    await HandleClientDisconnection(clientConnection.Username);
                }
                
                stream?.Close();
                tcpClient?.Close();
                Console.WriteLine($"[伺服器] 客戶端 {clientId} 連線已清理");
            }
        }

        /// <summary>
        /// 處理收到的訊息
        /// </summary>
        private async Task ProcessMessageAsync(ClientConnection clientConnection, string message)
        {
            var (messageType, parts) = MessageProtocol.ParseMessage(message);

            switch (messageType)
            {
                case MessageProtocol.LOGIN:
                    await HandleLoginAsync(clientConnection, parts);
                    break;

                case MessageProtocol.LOGOUT:
                    await HandleLogoutAsync(clientConnection, parts);
                    break;

                case MessageProtocol.BROADCAST:
                    await HandleBroadcastAsync(clientConnection, parts);
                    break;

                case MessageProtocol.PRIVATE:
                    await HandlePrivateMessageAsync(clientConnection, parts);
                    break;

                default:
                    await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("不支援的訊息類型"));
                    break;
            }
        }

        /// <summary>
        /// 處理使用者登入
        /// </summary>
        private async Task HandleLoginAsync(ClientConnection clientConnection, string[] parts)
        {
            if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0]))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("使用者名稱不能為空"));
                return;
            }

            string username = parts[0].Trim();

            // 檢查使用者名稱是否已存在
            if (_clients.ContainsKey(username))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("使用者名稱已存在"));
                return;
            }

            // 註冊使用者
            clientConnection.Username = username;
            _clients.TryAdd(username, clientConnection);

            Console.WriteLine($"[伺服器] 使用者 {username} 登入成功，目前線上人數: {_clients.Count}");

            // 發送登入成功訊息
            await SendToClientAsync(clientConnection, MessageProtocol.CreateSuccessMessage("登入成功"));

            // 廣播更新的使用者列表
            await BroadcastUserListAsync();

            // 廣播使用者加入訊息
            await BroadcastMessageAsync($"系統訊息：{username} 加入聊天室", username);
        }

        /// <summary>
        /// 處理使用者登出
        /// </summary>
        private async Task HandleLogoutAsync(ClientConnection clientConnection, string[] parts)
        {
            if (!string.IsNullOrEmpty(clientConnection.Username))
            {
                await HandleClientDisconnection(clientConnection.Username);
            }
        }

        /// <summary>
        /// 處理客戶端斷線
        /// </summary>
        private async Task HandleClientDisconnection(string username)
        {
            if (_clients.TryRemove(username, out _))
            {
                Console.WriteLine($"[伺服器] 使用者 {username} 已離線，目前線上人數: {_clients.Count}");

                // 廣播使用者離開訊息
                await BroadcastMessageAsync($"系統訊息：{username} 離開聊天室", username);

                // 廣播更新的使用者列表
                await BroadcastUserListAsync();
            }
        }

        /// <summary>
        /// 處理廣播訊息
        /// </summary>
        private async Task HandleBroadcastAsync(ClientConnection clientConnection, string[] parts)
        {
            if (string.IsNullOrEmpty(clientConnection.Username))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("請先登入"));
                return;
            }

            if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0]))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("訊息內容不能為空"));
                return;
            }

            string message = $"{clientConnection.Username}: {parts[0]}";
            await BroadcastMessageAsync(message, clientConnection.Username);
        }

        /// <summary>
        /// 處理私人訊息
        /// </summary>
        private async Task HandlePrivateMessageAsync(ClientConnection clientConnection, string[] parts)
        {
            if (string.IsNullOrEmpty(clientConnection.Username))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("請先登入"));
                return;
            }

            if (parts.Length < 2)
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage("私訊格式錯誤"));
                return;
            }

            string targetUsername = parts[0];
            string message = parts[1];

            if (!_clients.TryGetValue(targetUsername, out var targetClient))
            {
                await SendToClientAsync(clientConnection, MessageProtocol.CreateErrorMessage($"使用者 {targetUsername} 不在線上"));
                return;
            }

            string privateMessage = $"[私訊] {clientConnection.Username}: {message}";
            await SendToClientAsync(targetClient, MessageProtocol.CreateBroadcastMessage(privateMessage));

            // 也發送給發送者確認
            await SendToClientAsync(clientConnection, MessageProtocol.CreateSuccessMessage($"私訊已發送給 {targetUsername}"));
        }

        /// <summary>
        /// 廣播訊息給所有客戶端（除了指定的排除客戶端）
        /// </summary>
        private async Task BroadcastMessageAsync(string message, string? excludeUsername = null)
        {
            var broadcastMessage = MessageProtocol.CreateBroadcastMessage(message);
            var tasks = new List<Task>();

            foreach (var client in _clients.Values)
            {
                if (excludeUsername != null && client.Username == excludeUsername)
                    continue;

                tasks.Add(SendToClientAsync(client, broadcastMessage));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"[伺服器] 廣播訊息: {message}");
        }

        /// <summary>
        /// 廣播使用者列表給所有客戶端
        /// </summary>
        private async Task BroadcastUserListAsync()
        {
            string[] usernames = _clients.Keys.ToArray();
            string userListMessage = MessageProtocol.CreateUserListMessage(usernames);

            var tasks = new List<Task>();
            foreach (var client in _clients.Values)
            {
                tasks.Add(SendToClientAsync(client, userListMessage));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"[伺服器] 廣播使用者列表: {string.Join(", ", usernames)}");
        }

        /// <summary>
        /// 發送訊息給指定客戶端
        /// </summary>
        private async Task SendToClientAsync(ClientConnection client, string message)
        {
            try
            {
                if (client?.Stream != null && client.TcpClient.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                    await client.Stream.WriteAsync(data, 0, data.Length);
                    await client.Stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[伺服器] 發送訊息給客戶端 {client?.Username} 失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得目前線上使用者數量
        /// </summary>
        public int GetOnlineUserCount()
        {
            return _clients.Count;
        }

        /// <summary>
        /// 取得目前線上使用者列表
        /// </summary>
        public string[] GetOnlineUsers()
        {
            return _clients.Keys.ToArray();
        }
    }
} 