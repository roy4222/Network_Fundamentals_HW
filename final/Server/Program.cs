using System;
using System.Threading.Tasks;

namespace ChatServer
{
    /// <summary>
    /// 聊天伺服器主程式
    /// 提供控制台介面來啟動和管理伺服器
    /// </summary>
    class Program
    {
        private static TcpServer? _server;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("        功能增強的多人線上聊天室 - 伺服器");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            // 取得監聽埠
            int port = GetPortFromInput();
            
            try
            {
                // 建立並啟動伺服器
                _server = new TcpServer(port);
                
                // 設定 Ctrl+C 處理
                Console.CancelKeyPress += OnCancelKeyPress;
                
                Console.WriteLine("按 Ctrl+C 停止伺服器");
                Console.WriteLine();

                // 啟動伺服器（這會阻塞執行緒直到伺服器停止）
                var serverTask = _server.StartAsync();
                
                // 同時啟動指令監聽
                var commandTask = ListenForCommandsAsync();
                
                // 等待任一工作完成
                await Task.WhenAny(serverTask, commandTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"伺服器發生錯誤: {ex.Message}");
                Console.WriteLine("按任意鍵結束...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 從使用者輸入取得監聽埠
        /// </summary>
        private static int GetPortFromInput()
        {
            while (true)
            {
                Console.Write("請輸入伺服器監聽埠 (預設: 8888): ");
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    return 8888; // 預設埠
                }
                
                if (int.TryParse(input, out int port) && port > 0 && port <= 65535)
                {
                    return port;
                }
                
                Console.WriteLine("無效的埠號，請輸入 1-65535 之間的數字");
            }
        }

        /// <summary>
        /// 監聽控制台指令
        /// </summary>
        private static async Task ListenForCommandsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    Console.WriteLine("\n可用指令:");
                    Console.WriteLine("  'status' - 查看伺服器狀態");
                    Console.WriteLine("  'users'  - 查看線上使用者");
                    Console.WriteLine("  'stop'   - 停止伺服器");
                    Console.WriteLine("  'help'   - 顯示說明");
                    Console.Write("\n請輸入指令: ");

                    string? command = await Task.Run(() => Console.ReadLine());
                    
                    if (string.IsNullOrWhiteSpace(command))
                        continue;
                        
                    await ProcessCommandAsync(command.Trim().ToLower());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"處理指令時發生錯誤: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理控制台指令
        /// </summary>
        private static async Task ProcessCommandAsync(string command)
        {
            switch (command)
            {
                case "status":
                    ShowServerStatus();
                    break;

                case "users":
                    ShowOnlineUsers();
                    break;

                case "stop":
                    await StopServerAsync();
                    break;

                case "help":
                    ShowHelp();
                    break;

                default:
                    Console.WriteLine($"未知指令: {command}");
                    Console.WriteLine("輸入 'help' 查看可用指令");
                    break;
            }
        }

        /// <summary>
        /// 顯示伺服器狀態
        /// </summary>
        private static void ShowServerStatus()
        {
            if (_server != null)
            {
                Console.WriteLine("\n=== 伺服器狀態 ===");
                Console.WriteLine($"線上使用者數量: {_server.GetOnlineUserCount()}");
                Console.WriteLine($"伺服器啟動時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"狀態: {(_isRunning ? "運行中" : "已停止")}");
            }
            else
            {
                Console.WriteLine("伺服器尚未初始化");
            }
        }

        /// <summary>
        /// 顯示線上使用者
        /// </summary>
        private static void ShowOnlineUsers()
        {
            if (_server != null)
            {
                string[] users = _server.GetOnlineUsers();
                Console.WriteLine("\n=== 線上使用者 ===");
                
                if (users.Length == 0)
                {
                    Console.WriteLine("目前沒有使用者在線");
                }
                else
                {
                    Console.WriteLine($"共 {users.Length} 位使用者在線:");
                    for (int i = 0; i < users.Length; i++)
                    {
                        Console.WriteLine($"  {i + 1}. {users[i]}");
                    }
                }
            }
            else
            {
                Console.WriteLine("伺服器尚未初始化");
            }
        }

        /// <summary>
        /// 停止伺服器
        /// </summary>
        private static async Task StopServerAsync()
        {
            Console.WriteLine("\n正在停止伺服器...");
            _isRunning = false;
            
            _server?.Stop();
            
            await Task.Delay(1000); // 等待清理完成
            
            Console.WriteLine("伺服器已停止");
            Console.WriteLine("按任意鍵結束程式...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        /// <summary>
        /// 顯示說明
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("\n=== 伺服器指令說明 ===");
            Console.WriteLine("status  - 顯示伺服器目前狀態和統計資訊");
            Console.WriteLine("users   - 列出所有線上使用者");
            Console.WriteLine("stop    - 優雅地停止伺服器");
            Console.WriteLine("help    - 顯示此說明訊息");
            Console.WriteLine();
            Console.WriteLine("注意：使用者可以透過客戶端程式連線到此伺服器進行聊天");
        }

        /// <summary>
        /// 處理 Ctrl+C 事件
        /// </summary>
        private static async void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // 取消預設的程式終止行為
            Console.WriteLine("\n\n收到停止信號，正在優雅地關閉伺服器...");
            
            await StopServerAsync();
        }
    }
} 