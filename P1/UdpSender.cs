using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PMessenger
{
    /// <summary>
    /// UDP訊息發送器
    /// 負責將文字訊息發送到指定的IP和端口
    /// </summary>
    public class UdpSender : IDisposable
    {
        private UdpClient? udpClient;
        private bool disposed = false;

        /// <summary>
        /// 初始化UDP發送器
        /// </summary>
        public UdpSender()
        {
            try
            {
                udpClient = new UdpClient();
                Console.WriteLine("[UdpSender] UDP發送器初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UdpSender] 初始化失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 發送文字訊息到指定的遠端端點
        /// </summary>
        /// <param name="message">要發送的訊息內容</param>
        /// <param name="remoteIp">目標IP地址</param>
        /// <param name="remotePort">目標端口</param>
        /// <returns>發送是否成功</returns>
        public async Task<bool> SendMessageAsync(string message, string remoteIp, int remotePort)
        {
            if (disposed || udpClient == null)
            {
                Console.WriteLine("[UdpSender] 發送器已釋放，無法發送訊息");
                return false;
            }

            try
            {
                // 將訊息轉換為UTF-8位元組陣列
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                
                // 創建遠端端點
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                
                // 發送訊息
                int bytesSent = await udpClient.SendAsync(messageBytes, messageBytes.Length, remoteEndPoint);
                
                Console.WriteLine($"[UdpSender] 發送訊息成功: {bytesSent} 位元組 -> {remoteIp}:{remotePort}");
                Console.WriteLine($"[UdpSender] 訊息內容: {message}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UdpSender] 發送訊息失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步發送訊息（包裝非同步方法）
        /// </summary>
        /// <param name="message">要發送的訊息內容</param>
        /// <param name="remoteIp">目標IP地址</param>
        /// <param name="remotePort">目標端口</param>
        /// <returns>發送是否成功</returns>
        public bool SendMessage(string message, string remoteIp, int remotePort)
        {
            return SendMessageAsync(message, remoteIp, remotePort).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    udpClient?.Close();
                    udpClient?.Dispose();
                    Console.WriteLine("[UdpSender] UDP發送器已釋放");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UdpSender] 釋放資源時發生錯誤: {ex.Message}");
                }
                finally
                {
                    disposed = true;
                }
            }
        }
    }
} 