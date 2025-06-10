using System;
using System.Threading.Tasks;

namespace P2PMessenger
{
    /// <summary>
    /// 基本UDP通訊測試類別
    /// 用於驗證UdpSender和UdpListener的基本功能
    /// 提供自動化測試以確保P2P通訊框架的正確性
    /// </summary>
    public static class TestUdpBasic
    {
        /// <summary>
        /// 執行基本的UDP通訊測試
        /// 這個方法會測試完整的UDP收發流程：
        /// 1. 初始化監聽器和發送器
        /// 2. 啟動監聽服務
        /// 3. 發送測試訊息
        /// 4. 驗證訊息接收
        /// 5. 清理資源
        /// </summary>
        /// <returns>測試是否成功通過</returns>
        public static async Task<bool> RunBasicTestAsync()
        {
            Console.WriteLine("=== 開始基本UDP通訊測試 ===");
            
            // 初始化測試所需的物件變數
            UdpListener? listener = null;
            UdpSender? sender = null;
            bool testPassed = false;
            
            try
            {
                // === 測試配置區 ===
                // 使用9999端口進行測試，避免與應用程式主要功能衝突
                int testPort = 9999;
                string testMessage = "Hello UDP P2P Test!";
                string testIp = "127.0.0.1"; // 使用本地回環地址進行測試
                
                // === 第一階段：測試監聽器初始化 ===
                Console.WriteLine("1. 測試監聽器初始化...");
                listener = new UdpListener(testPort);
                
                // 用於追蹤訊息接收狀態的變數
                bool messageReceived = false;
                string receivedMessage = "";
                
                // 設定事件處理器來捕獲接收到的訊息
                // 當UdpListener接收到訊息時，會觸發此事件
                listener.MessageReceived += (message, senderIp, senderPort) =>
                {
                    Console.WriteLine($"   收到測試訊息: {message}");
                    Console.WriteLine($"   來源: {senderIp}:{senderPort}");
                    receivedMessage = message;
                    messageReceived = true; // 標記已收到訊息
                };
                
                // === 第二階段：啟動監聽服務 ===
                Console.WriteLine("2. 啟動監聽...");
                bool listenerStarted = await listener.StartListeningAsync();
                
                // 檢查監聽器是否成功啟動
                if (!listenerStarted)
                {
                    Console.WriteLine("   ❌ 監聽器啟動失敗");
                    return false;
                }
                
                Console.WriteLine("   ✅ 監聽器啟動成功");
                
                // === 第三階段：測試發送器初始化 ===
                Console.WriteLine("3. 測試發送器初始化...");
                sender = new UdpSender();
                Console.WriteLine("   ✅ 發送器初始化成功");
                
                // === 第四階段：發送測試訊息 ===
                Console.WriteLine("4. 發送測試訊息...");
                bool sendSuccess = await sender.SendMessageAsync(testMessage, testIp, testPort);
                
                // 檢查訊息是否成功發送
                if (!sendSuccess)
                {
                    Console.WriteLine("   ❌ 訊息發送失敗");
                    return false;
                }
                
                Console.WriteLine("   ✅ 訊息發送成功");
                
                // === 第五階段：等待並驗證訊息接收 ===
                Console.WriteLine("5. 等待接收訊息...");
                
                // 等待最多3秒讓訊息能夠被接收和處理
                // 使用輪詢方式檢查，每100毫秒檢查一次
                for (int i = 0; i < 30; i++)
                {
                    if (messageReceived)
                        break; // 如果已收到訊息，提前跳出等待迴圈
                    await Task.Delay(100); // 等待100毫秒後再次檢查
                }
                
                // 驗證訊息接收結果
                if (messageReceived && receivedMessage == testMessage)
                {
                    Console.WriteLine("   ✅ 訊息接收成功，內容正確");
                    testPassed = true;
                }
                else
                {
                    Console.WriteLine("   ❌ 訊息接收失敗或內容不正確");
                    Console.WriteLine($"   預期: {testMessage}");
                    Console.WriteLine($"   實際: {receivedMessage}");
                }
                
                // === 第六階段：測試停止監聽功能 ===
                Console.WriteLine("6. 測試停止監聽...");
                await listener.StopListeningAsync();
                Console.WriteLine("   ✅ 監聽器停止成功");
                
            }
            catch (Exception ex)
            {
                // 捕獲測試過程中的任何異常
                Console.WriteLine($"❌ 測試過程中發生例外: {ex.Message}");
                testPassed = false;
            }
            finally
            {
                // === 資源清理階段 ===
                // 無論測試成功或失敗，都要確保資源被正確釋放
                try
                {
                    listener?.Dispose(); // 釋放監聽器資源
                    sender?.Dispose();   // 釋放發送器資源
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理資源時發生錯誤: {ex.Message}");
                }
            }
            
            // === 測試結果報告 ===
            Console.WriteLine("=== UDP通訊測試結果 ===");
            if (testPassed)
            {
                Console.WriteLine("🎉 所有測試通過！基本UDP通訊功能正常");
            }
            else
            {
                Console.WriteLine("❌ 測試失敗，請檢查網路設定或防火牆");
            }
            
            return testPassed;
        }
        
        /// <summary>
        /// 執行延遲測試
        /// 此方法測量UDP訊息的傳輸延遲，用於評估網路性能
        /// 通過發送多個PING訊息並測量往返時間來計算網路延遲
        /// </summary>
        public static async Task RunLatencyTestAsync()
        {
            Console.WriteLine("\n=== 開始延遲測試 ===");
            
            // 初始化測試所需的物件
            UdpListener? listener = null;
            UdpSender? sender = null;
            
            try
            {
                // === 延遲測試配置 ===
                int testPort = 9998; // 使用不同端口避免與基本測試衝突
                string testIp = "127.0.0.1";
                
                // 初始化監聽器和發送器
                listener = new UdpListener(testPort);
                sender = new UdpSender();
                
                // 用於測量延遲的變數
                DateTime sendTime = DateTime.MinValue; // 記錄發送時間
                bool responseReceived = false;         // 標記是否收到回應
                
                // 設定訊息接收事件處理器
                // 當收到PING訊息時，計算延遲時間
                listener.MessageReceived += (message, senderIp, senderPort) =>
                {
                    // 檢查是否為PING測試訊息
                    if (message.StartsWith("PING_"))
                    {
                        // 計算從發送到接收的時間差
                        var latency = DateTime.Now - sendTime;
                        Console.WriteLine($"   延遲: {latency.TotalMilliseconds:F2} 毫秒");
                        responseReceived = true; // 標記已收到回應
                    }
                };
                
                // 啟動監聽服務
                await listener.StartListeningAsync();
                
                // === 執行延遲測試迴圈 ===
                // 發送10次PING測試，統計延遲數據
                for (int i = 1; i <= 10; i++)
                {
                    // 重置回應狀態
                    responseReceived = false;
                    sendTime = DateTime.Now; // 記錄發送時間
                    
                    // 創建包含時間戳的PING訊息
                    // 格式: PING_序號_時間戳
                    string pingMessage = $"PING_{i}_{sendTime.Ticks}";
                    
                    // 發送PING訊息
                    await sender.SendMessageAsync(pingMessage, testIp, testPort);
                    
                    // === 等待回應階段 ===
                    // 等待回應（最多500毫秒），每10毫秒檢查一次
                    for (int j = 0; j < 50; j++)
                    {
                        if (responseReceived) break; // 收到回應則提前結束等待
                        await Task.Delay(10); // 等待10毫秒
                    }
                    
                    // 如果在超時時間內沒有收到回應
                    if (!responseReceived)
                    {
                        Console.WriteLine($"   PING {i}: 逾時");
                    }
                    
                    // 在下一次PING之前等待200毫秒，避免網路擁塞
                    await Task.Delay(200);
                }
                
                // 停止監聽服務
                await listener.StopListeningAsync();
            }
            catch (Exception ex)
            {
                // 捕獲延遲測試過程中的異常
                Console.WriteLine($"延遲測試發生錯誤: {ex.Message}");
            }
            finally
            {
                // === 清理資源 ===
                // 確保測試完成後釋放所有網路資源
                listener?.Dispose();
                sender?.Dispose();
            }
            
            Console.WriteLine("=== 延遲測試完成 ===");
        }
    }
} 