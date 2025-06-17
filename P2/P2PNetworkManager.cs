using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P2PMessenger
{
    /// <summary>
    /// P2P 網路管理器
    /// 實現點對點通訊，同時具備 Sender 和 Listener 功能
    /// 根據 EXP2 設計講義實現雙向筆跡資料傳輸
    /// </summary>
    public class P2PNetworkManager : IDisposable
    {
        #region 私有欄位

        // TCP 伺服器 (Listener 功能)
        private TcpListener tcpListener = null!;
        private bool isListening = false;
        private CancellationTokenSource listenerCancellationToken = null!;

        // TCP 客戶端 (Sender 功能)
        private TcpClient tcpClient = null!;
        private NetworkStream clientStream = null!;
        private bool isConnected = false;

        // 網路設定
        private string localIP = null!;
        private string remoteIP = null!;
        private int port;

        // 執行緒同步
        private readonly object lockObject = new object();

        #endregion

        #region 事件

        /// <summary>
        /// 資料接收事件
        /// 當接收到遠端筆跡資料時觸發
        /// </summary>
        public event EventHandler<StrokeDataEventArgs>? DataReceived;

        /// <summary>
        /// 連線狀態變更事件
        /// 當連線狀態改變時觸發
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

        #endregion

        #region 公開屬性

        /// <summary>
        /// 是否已連線
        /// </summary>
        public bool IsConnected => isConnected;

        /// <summary>
        /// 是否正在監聽
        /// </summary>
        public bool IsListening => isListening;

        /// <summary>
        /// 本機 IP 位址
        /// </summary>
        public string LocalIP => localIP;

        /// <summary>
        /// 遠端 IP 位址
        /// </summary>
        public string RemoteIP => remoteIP;

        /// <summary>
        /// 通訊埠號
        /// </summary>
        public int Port => port;

        #endregion

        #region 建構函式和初始化

        /// <summary>
        /// 建構函式
        /// </summary>
        public P2PNetworkManager()
        {
            Console.WriteLine("P2P 網路管理器初始化");
        }

        #endregion

        #region 連線管理

        /// <summary>
        /// 開始 P2P 連線
        /// 同時啟動 Listener (伺服器) 和 Sender (客戶端) 功能
        /// </summary>
        /// <param name="localIP">本機 IP 位址</param>
        /// <param name="remoteIP">遠端 IP 位址</param>
        /// <param name="port">通訊埠號</param>
        public void StartConnection(string localIP, string remoteIP, int port)
        {
            this.localIP = localIP;
            this.remoteIP = remoteIP;
            this.port = port;

            Console.WriteLine($"開始 P2P 連線: 本機={localIP}:{port}, 遠端={remoteIP}:{port}");

            // 在背景執行緒中執行連線程序
            _ = Task.Run(async () =>
            {
                try
                {
                    // 1. 啟動 Listener (接收功能)
                    await StartListener();

                    // 2. 延遲一下讓 Listener 完全啟動
                    await Task.Delay(500);

                    // 3. 嘗試連線到遠端 (傳送功能)
                    await ConnectToRemote();

                    OnConnectionStatusChanged(true, "P2P 連線成功", remoteIP);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"P2P 連線失敗: {ex.Message}");
                    OnConnectionStatusChanged(false, $"連線失敗: {ex.Message}", remoteIP);
                }
            });
        }

        /// <summary>
        /// 停止 P2P 連線
        /// 關閉 Listener 和 Sender 功能
        /// </summary>
        public void StopConnection()
        {
            Console.WriteLine("停止 P2P 連線");

            lock (lockObject)
            {
                // 停止 Listener
                StopListener();

                // 停止 Sender
                DisconnectFromRemote();

                isConnected = false;
            }

            OnConnectionStatusChanged(false, "已斷線", "");
        }

        #endregion

        #region Listener 功能 (接收資料)

        /// <summary>
        /// 啟動 TCP Listener
        /// 監聽來自遠端的連線請求
        /// </summary>
        private Task StartListener()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(localIP);
                tcpListener = new TcpListener(ipAddress, port);
                tcpListener.Start();

                isListening = true;
                listenerCancellationToken = new CancellationTokenSource();

                Console.WriteLine($"TCP Listener 已啟動，監聽 {localIP}:{port}");

                // 在背景執行緒中處理連線請求
                _ = Task.Run(async () => await AcceptClientsAsync(listenerCancellationToken.Token));
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"啟動 Listener 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 接受客戶端連線的非同步方法
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && isListening)
            {
                try
                {
                    Console.WriteLine("等待客戶端連線...");
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    
                    string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知";
                    Console.WriteLine($"客戶端已連線: {clientEndpoint}");

                    // 為每個客戶端建立處理執行緒
                    _ = Task.Run(async () => await HandleClientAsync(client, cancellationToken));
                }
                catch (ObjectDisposedException)
                {
                    // Listener 已關閉，正常結束
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"接受客戶端連線時發生錯誤: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 處理客戶端連線和資料接收
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream? stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        // 客戶端已斷線
                        break;
                    }

                    // 處理接收到的資料
                    string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "";
                    await ProcessReceivedData(buffer, bytesRead, clientEndpoint);
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"處理客戶端資料時發生錯誤: {ex.Message}");
                }
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Console.WriteLine("客戶端連線已關閉");
            }
        }

        /// <summary>
        /// 處理接收到的資料
        /// 實現「資料→筆跡記錄器」功能
        /// </summary>
        private Task ProcessReceivedData(byte[] buffer, int length, string sourceIP)
        {
            try
            {
                string jsonData = Encoding.UTF8.GetString(buffer, 0, length);
                StrokeData? strokeData = JsonSerializer.Deserialize<StrokeData>(jsonData);

                if (strokeData != null && strokeData.IsValid())
                {
                    Console.WriteLine($"接收到筆跡資料: {strokeData}");
                    OnDataReceived(strokeData, sourceIP);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理接收資料時發生錯誤: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止 TCP Listener
        /// </summary>
        private void StopListener()
        {
            try
            {
                isListening = false;
                listenerCancellationToken?.Cancel();
                tcpListener?.Stop();
                
                Console.WriteLine("TCP Listener 已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止 Listener 時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region Sender 功能 (傳送資料)

        /// <summary>
        /// 連線到遠端伺服器
        /// </summary>
        private async Task ConnectToRemote()
        {
            try
            {
                tcpClient = new TcpClient();
                
                // 設定連線逾時
                var connectTask = tcpClient.ConnectAsync(remoteIP, port);
                var timeoutTask = Task.Delay(5000); // 5 秒逾時

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("連線逾時");
                }

                if (tcpClient.Connected)
                {
                    clientStream = tcpClient.GetStream();
                    isConnected = true;
                    
                    Console.WriteLine($"已連線到遠端伺服器: {remoteIP}:{port}");
                }
                else
                {
                    throw new Exception("無法建立連線");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"連線到遠端失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 中斷與遠端的連線
        /// </summary>
        private void DisconnectFromRemote()
        {
            try
            {
                clientStream?.Close();
                tcpClient?.Close();
                isConnected = false;
                
                Console.WriteLine("已中斷與遠端的連線");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"中斷連線時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 傳送筆跡資料到遠端
        /// 實現「筆跡→資料記錄器」功能
        /// </summary>
        /// <param name="strokeData">要傳送的筆跡資料</param>
        public void SendStrokeData(StrokeData strokeData)
        {
            if (!isConnected || clientStream == null || !strokeData.IsValid())
            {
                return;
            }

            // 在背景執行緒中傳送資料
            _ = Task.Run(async () =>
            {
                try
                {
                    // 將筆跡資料序列化為 JSON
                    string jsonData = JsonSerializer.Serialize(strokeData);
                    byte[] data = Encoding.UTF8.GetBytes(jsonData);

                    // 傳送資料
                    await clientStream.WriteAsync(data, 0, data.Length);
                    await clientStream.FlushAsync();

                    Console.WriteLine($"已傳送筆跡資料: {strokeData}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"傳送筆跡資料失敗: {ex.Message}");
                    
                    // 連線可能已中斷，嘗試重新連線
                    _ = Task.Run(async () => await TryReconnect());
                }
            });
        }

        /// <summary>
        /// 嘗試重新連線
        /// </summary>
        private async Task TryReconnect()
        {
            if (isConnected) return;

            try
            {
                Console.WriteLine("嘗試重新連線...");
                DisconnectFromRemote();
                await Task.Delay(2000); // 等待 2 秒
                await ConnectToRemote();
                
                OnConnectionStatusChanged(true, "重新連線成功", remoteIP);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重新連線失敗: {ex.Message}");
                OnConnectionStatusChanged(false, $"重新連線失敗: {ex.Message}", remoteIP);
            }
        }

        #endregion

        #region 事件觸發

        /// <summary>
        /// 觸發資料接收事件
        /// </summary>
        protected virtual void OnDataReceived(StrokeData strokeData, string sourceIP)
        {
            DataReceived?.Invoke(this, new StrokeDataEventArgs(strokeData, sourceIP));
        }

        /// <summary>
        /// 觸發連線狀態變更事件
        /// </summary>
        protected virtual void OnConnectionStatusChanged(bool isConnected, string message, string remoteIP)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(isConnected, message, remoteIP));
        }

        #endregion

        #region IDisposable 實作

        private bool disposed = false;

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 釋放資源的實際實作
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    StopConnection();
                    listenerCancellationToken?.Dispose();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// 解構函式
        /// </summary>
        ~P2PNetworkManager()
        {
            Dispose(false);
        }

        #endregion
    }
} 