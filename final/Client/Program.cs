using System;
using System.Threading.Tasks;

namespace ChatClient
{
    /// <summary>
    /// 聊天客戶端主程式
    /// 提供控制台介面來連線伺服器和測試基本功能
    /// </summary>
    class Program
    {
        private static TcpClient? _client;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("        功能增強的多人線上聊天室 - 客戶端");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            try
            {
                // 取得伺服器連線資訊
                var (serverAddress, serverPort) = GetServerInfo();
                
                // 建立客戶端
                _client = new TcpClient(serverAddress, serverPort);
                
                // 註冊事件處理器
                RegisterEventHandlers();
                
                // 設定 Ctrl+C 處理
                Console.CancelKeyPress += OnCancelKeyPress;
                
                // 嘗試連線到伺服器
                if (await _client.ConnectAsync())
                {
                    Console.WriteLine("連線成功！");
                    
                    // 登入
                    await LoginAsync();
                    
                    // 開始互動式聊天
                    await StartChatAsync();
                }
                else
                {
                    Console.WriteLine("連線失敗，請檢查伺服器是否啟動");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"客戶端發生錯誤: {ex.Message}");
            }
            finally
            {
                if (_client?.IsConnected == true)
                {
                    await _client.LogoutAsync();
                }
                
                Console.WriteLine("按任意鍵結束...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 取得伺服器連線資訊
        /// </summary>
        private static (string address, int port) GetServerInfo()
        {
            Console.Write("請輸入伺服器地址 (預設: localhost): ");
            string? address = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(address))
            {
                address = "localhost";
            }

            Console.Write("請輸入伺服器埠號 (預設: 8888): ");
            string? portInput = Console.ReadLine();
            int port = 8888;
            
            if (!string.IsNullOrWhiteSpace(portInput) && int.TryParse(portInput, out int parsedPort))
            {
                port = parsedPort;
            }

            return (address, port);
        }

        /// <summary>
        /// 註冊事件處理器
        /// </summary>
        private static void RegisterEventHandlers()
        {
            if (_client == null) return;

            _client.MessageReceived += OnMessageReceived;
            _client.UserListUpdated += OnUserListUpdated;
            _client.ErrorOccurred += OnErrorOccurred;
            _client.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// 登入流程
        /// </summary>
        private static async Task LoginAsync()
        {
            while (_client?.IsConnected == true)
            {
                Console.Write("\n請輸入您的使用者名稱: ");
                string? username = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("使用者名稱不能為空，請重新輸入");
                    continue;
                }

                if (await _client.LoginAsync(username))
                {
                    Console.WriteLine($"歡迎 {username}！");
                    break;
                }
                else
                {
                    Console.WriteLine("登入失敗，請重新輸入使用者名稱");
                }
            }
        }

        /// <summary>
        /// 開始互動式聊天
        /// </summary>
        private static async Task StartChatAsync()
        {
            Console.WriteLine("\n=== 聊天室 ===");
            Console.WriteLine("指令說明:");
            Console.WriteLine("  輸入訊息並按 Enter 發送廣播訊息");
            Console.WriteLine("  '/private <使用者名稱> <訊息>' 發送私訊");
            Console.WriteLine("  '/quit' 離開聊天室");
            Console.WriteLine("  '/help' 顯示說明");
            Console.WriteLine();

            while (_client?.IsConnected == true && _isRunning)
            {
                try
                {
                    Console.Write($"[{_client.Username}] ");
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    await ProcessUserInputAsync(input.Trim());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"處理輸入時發生錯誤: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理使用者輸入
        /// </summary>
        private static async Task ProcessUserInputAsync(string input)
        {
            if (_client == null || !_client.IsConnected) return;

            if (input.StartsWith("/"))
            {
                // 處理指令
                await ProcessCommandAsync(input);
            }
            else
            {
                // 發送廣播訊息
                await _client.SendBroadcastMessageAsync(input);
            }
        }

        /// <summary>
        /// 處理指令
        /// </summary>
        private static async Task ProcessCommandAsync(string command)
        {
            if (_client == null) return;

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            switch (parts[0].ToLower())
            {
                case "/private":
                    if (parts.Length >= 3)
                    {
                        string targetUser = parts[1];
                        string message = string.Join(" ", parts, 2, parts.Length - 2);
                        await _client.SendPrivateMessageAsync(targetUser, message);
                    }
                    else
                    {
                        Console.WriteLine("私訊格式: /private <使用者名稱> <訊息>");
                    }
                    break;

                case "/quit":
                    Console.WriteLine("正在離開聊天室...");
                    _isRunning = false;
                    await _client.LogoutAsync();
                    break;

                case "/help":
                    ShowHelp();
                    break;

                default:
                    Console.WriteLine($"未知指令: {parts[0]}");
                    Console.WriteLine("輸入 '/help' 查看可用指令");
                    break;
            }
        }

        /// <summary>
        /// 顯示說明
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("\n=== 聊天室指令說明 ===");
            Console.WriteLine("廣播訊息    : 直接輸入訊息內容");
            Console.WriteLine("私人訊息    : /private <使用者名稱> <訊息>");
            Console.WriteLine("離開聊天室  : /quit");
            Console.WriteLine("顯示說明    : /help");
            Console.WriteLine();
        }

        // === 事件處理器 ===

        private static void OnMessageReceived(string message)
        {
            Console.WriteLine($"\n>>> {message}");
            Console.Write($"[{_client?.Username}] ");
        }

        private static void OnUserListUpdated(string[] users)
        {
            Console.WriteLine($"\n--- 線上使用者 ({users.Length}): {string.Join(", ", users)} ---");
            Console.Write($"[{_client?.Username}] ");
        }

        private static void OnErrorOccurred(string error)
        {
            Console.WriteLine($"\n❌ 錯誤: {error}");
            Console.Write($"[{_client?.Username}] ");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine("\n--- 與伺服器的連線已中斷 ---");
            _isRunning = false;
        }

        /// <summary>
        /// 處理 Ctrl+C 事件
        /// </summary>
        private static async void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // 取消預設的程式終止行為
            Console.WriteLine("\n\n收到停止信號，正在退出...");
            
            _isRunning = false;
            
            if (_client?.IsConnected == true)
            {
                await _client.LogoutAsync();
            }
            
            Environment.Exit(0);
        }
    }
} 