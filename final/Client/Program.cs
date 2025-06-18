using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    /// <summary>
    /// 聊天客戶端主程式
    /// 支援圖形化介面和控制台模式
    /// </summary>
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("        功能增強的多人線上聊天室 - 客戶端");
            Console.WriteLine("        里程碑 2：使用者系統與群聊功能");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            // 檢查是否要使用控制台模式
            bool useConsoleMode = args.Length > 0 && args[0].ToLower() == "--console";

            if (useConsoleMode)
            {
                Console.WriteLine("啟動控制台模式...");
                await RunConsoleMode();
            }
            else
            {
                Console.WriteLine("啟動圖形化介面...");
                RunGuiMode();
            }
        }

        /// <summary>
        /// 啟動圖形化介面模式
        /// </summary>
        private static void RunGuiMode()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                var chatForm = new ChatForm();
                Application.Run(chatForm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"圖形化介面啟動失敗: {ex.Message}");
                Console.WriteLine("正在切換到控制台模式...");
                RunConsoleMode().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 啟動控制台模式
        /// </summary>
        private static async Task RunConsoleMode()
        {
            TcpClient? _client = null;
            bool _isRunning = true;

            try
            {
                // 取得伺服器連線資訊
                var (serverAddress, serverPort) = GetServerInfo();
                
                // 建立客戶端
                _client = new TcpClient(serverAddress, serverPort);
                
                // 註冊事件處理器
                RegisterEventHandlers(_client);
                
                // 設定 Ctrl+C 處理
                Console.CancelKeyPress += async (sender, e) =>
                {
                    e.Cancel = true;
                    _isRunning = false;
                    if (_client?.IsConnected == true)
                    {
                        await _client.LogoutAsync();
                    }
                    Environment.Exit(0);
                };
                
                // 嘗試連線到伺服器
                if (await _client.ConnectAsync())
                {
                    Console.WriteLine("連線成功！");
                    
                    // 登入
                    await LoginAsync(_client);
                    
                    // 開始互動式聊天
                    await StartChatAsync(_client, () => _isRunning);
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
        private static void RegisterEventHandlers(TcpClient client)
        {
            client.MessageReceived += OnMessageReceived;
            client.UserListUpdated += OnUserListUpdated;
            client.ErrorOccurred += OnErrorOccurred;
            client.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// 登入流程
        /// </summary>
        private static async Task LoginAsync(TcpClient client)
        {
            while (client?.IsConnected == true)
            {
                Console.Write("\n請輸入您的使用者名稱: ");
                string? username = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("使用者名稱不能為空，請重新輸入");
                    continue;
                }

                if (await client.LoginAsync(username))
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
        private static async Task StartChatAsync(TcpClient client, Func<bool> isRunning)
        {
            Console.WriteLine("\n=== 聊天室 ===");
            Console.WriteLine("指令說明:");
            Console.WriteLine("  輸入訊息並按 Enter 發送廣播訊息");
            Console.WriteLine("  '/private <使用者名稱> <訊息>' 發送私訊");
            Console.WriteLine("  '/quit' 離開聊天室");
            Console.WriteLine("  '/help' 顯示說明");
            Console.WriteLine();

            while (client?.IsConnected == true && isRunning())
            {
                try
                {
                    Console.Write($"[{client.Username}] ");
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    await ProcessUserInputAsync(client, input.Trim());
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
        private static async Task ProcessUserInputAsync(TcpClient client, string input)
        {
            if (!client.IsConnected) return;

            if (input.StartsWith("/"))
            {
                // 處理指令
                await ProcessCommandAsync(client, input);
            }
            else
            {
                // 發送廣播訊息
                await client.SendBroadcastMessageAsync(input);
            }
        }

        /// <summary>
        /// 處理指令
        /// </summary>
        private static async Task ProcessCommandAsync(TcpClient client, string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            switch (parts[0].ToLower())
            {
                case "/private":
                    if (parts.Length >= 3)
                    {
                        string targetUser = parts[1];
                        string message = string.Join(" ", parts, 2, parts.Length - 2);
                        await client.SendPrivateMessageAsync(targetUser, message);
                    }
                    else
                    {
                        Console.WriteLine("私訊格式: /private <使用者名稱> <訊息>");
                    }
                    break;

                case "/quit":
                    Console.WriteLine("正在離開聊天室...");
                    await client.LogoutAsync();
                    Environment.Exit(0);
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
            Console.Write($"[輸入] ");
        }

        private static void OnUserListUpdated(string[] users)
        {
            Console.WriteLine($"\n--- 線上使用者 ({users.Length}): {string.Join(", ", users)} ---");
            Console.Write($"[輸入] ");
        }

        private static void OnErrorOccurred(string error)
        {
            Console.WriteLine($"\n❌ 錯誤: {error}");
            Console.Write($"[輸入] ");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine("\n--- 與伺服器的連線已中斷 ---");
        }


    }
} 