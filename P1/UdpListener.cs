using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PMessenger
{
    /// <summary>
    /// UDP訊息監聽器
    /// 負責在指定端口監聽來自其他節點的訊息
    /// 使用多線程處理，避免阻塞主UI線程
    /// </summary>
    public class UdpListener : IDisposable
    {
        private UdpClient? udpClient;
        private bool isListening = false;
        private bool disposed = false;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? listeningTask;
        private readonly int localPort;

        /// <summary>
        /// 訊息接收事件
        /// </summary>
        public event Action<string, string, int>? MessageReceived;

        /// <summary>
        /// 監聽狀態變更事件
        /// </summary>
        public event Action<bool>? ListeningStatusChanged;

        /// <summary>
        /// 初始化UDP監聽器
        /// </summary>
        /// <param name="port">本地監聽端口</param>
        public UdpListener(int port)
        {
            localPort = port;
            Console.WriteLine($"[UdpListener] 監聽器初始化，端口: {port}");
        }

        /// <summary>
        /// 開始監聽
        /// </summary>
        /// <returns>是否成功開始監聽</returns>
        public Task<bool> StartListeningAsync()
        {
            if (isListening || disposed)
            {
                Console.WriteLine("[UdpListener] 監聽器已在運行或已釋放");
                return Task.FromResult(false);
            }

            try
            {
                // 創建UDP客戶端並綁定到指定端口
                udpClient = new UdpClient(localPort);
                cancellationTokenSource = new CancellationTokenSource();
                
                // 啟動監聽任務（獨立線程）
                listeningTask = Task.Run(() => ListenForMessagesAsync(cancellationTokenSource.Token));
                
                isListening = true;
                ListeningStatusChanged?.Invoke(true);
                
                Console.WriteLine($"[UdpListener] 開始監聽端口 {localPort}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UdpListener] 啟動監聽失敗: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 停止監聽
        /// </summary>
        public async Task StopListeningAsync()
        {
            if (!isListening)
            {
                Console.WriteLine("[UdpListener] 監聽器未在運行");
                return;
            }

            try
            {
                isListening = false;
                
                // 取消監聽任務
                cancellationTokenSource?.Cancel();
                
                // 等待監聽任務結束（最多等待3秒）
                if (listeningTask != null)
                {
                    await Task.WhenAny(listeningTask, Task.Delay(3000));
                }
                
                // 關閉UDP客戶端
                udpClient?.Close();
                udpClient?.Dispose();
                udpClient = null;
                
                ListeningStatusChanged?.Invoke(false);
                Console.WriteLine("[UdpListener] 停止監聽");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UdpListener] 停止監聽時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 監聽訊息的主要方法（在獨立線程中運行）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[UdpListener] 監聽線程啟動");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && udpClient != null)
                {
                    try
                    {
                        // 接收訊息（非阻塞）
                        var result = await udpClient.ReceiveAsync();
                        
                        if (result.Buffer != null && result.Buffer.Length > 0)
                        {
                            // 將位元組陣列轉換為字串
                            string message = Encoding.UTF8.GetString(result.Buffer);
                            string senderIp = result.RemoteEndPoint.Address.ToString();
                            int senderPort = result.RemoteEndPoint.Port;
                            
                            Console.WriteLine($"[UdpListener] 收到訊息: {message}");
                            Console.WriteLine($"[UdpListener] 來源: {senderIp}:{senderPort}");
                            
                            // 觸發訊息接收事件
                            MessageReceived?.Invoke(message, senderIp, senderPort);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // UDP客戶端已被釋放，正常退出
                        break;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                    {
                        // 監聽被中斷，正常退出
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UdpListener] 接收訊息時發生錯誤: {ex.Message}");
                        
                        // 短暫延遲後繼續監聽
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[UdpListener] 監聽被取消");
            }
            finally
            {
                Console.WriteLine("[UdpListener] 監聽線程結束");
            }
        }

        /// <summary>
        /// 取得監聽狀態
        /// </summary>
        public bool IsListening => isListening;

        /// <summary>
        /// 取得本地端口
        /// </summary>
        public int LocalPort => localPort;

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    StopListeningAsync().GetAwaiter().GetResult();
                    cancellationTokenSource?.Dispose();
                    Console.WriteLine("[UdpListener] UDP監聽器已釋放");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UdpListener] 釋放資源時發生錯誤: {ex.Message}");
                }
                finally
                {
                    disposed = true;
                }
            }
        }
    }
} 