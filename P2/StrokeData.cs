using System;
using System.Drawing;

namespace P2PMessenger
{
    /// <summary>
    /// 筆跡資料結構
    /// 用於記錄和傳輸繪圖筆跡資訊
    /// 實現「筆跡→資料記錄器」功能
    /// </summary>
    [Serializable]
    public class StrokeData
    {
        /// <summary>
        /// 筆跡起始點座標
        /// </summary>
        public Point StartPoint { get; set; }

        /// <summary>
        /// 筆跡結束點座標
        /// </summary>
        public Point EndPoint { get; set; }

        /// <summary>
        /// 筆跡時間戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 筆跡顏色
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// 筆跡寬度
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 筆跡類型（線條、點等）
        /// </summary>
        public StrokeType Type { get; set; } = StrokeType.Line;

        /// <summary>
        /// 建構函式
        /// </summary>
        public StrokeData()
        {
            Timestamp = DateTime.Now;
            Color = Color.Black;
            Width = 2.0f;
        }

        /// <summary>
        /// 建構函式（含參數）
        /// </summary>
        /// <param name="startPoint">起始點</param>
        /// <param name="endPoint">結束點</param>
        /// <param name="color">顏色</param>
        /// <param name="width">寬度</param>
        public StrokeData(Point startPoint, Point endPoint, Color color, float width)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Color = color;
            Width = width;
            Timestamp = DateTime.Now;
            Type = StrokeType.Line;
        }

        /// <summary>
        /// 轉換為字串表示
        /// </summary>
        /// <returns>筆跡資料的字串描述</returns>
        public override string ToString()
        {
            return $"Stroke: ({StartPoint.X},{StartPoint.Y}) -> ({EndPoint.X},{EndPoint.Y}), " +
                   $"Color: {Color.Name}, Width: {Width}, Time: {Timestamp:HH:mm:ss.fff}";
        }

        /// <summary>
        /// 計算筆跡長度
        /// </summary>
        /// <returns>筆跡的像素長度</returns>
        public double GetLength()
        {
            int deltaX = EndPoint.X - StartPoint.X;
            int deltaY = EndPoint.Y - StartPoint.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// 檢查筆跡是否有效（起始點和結束點不同）
        /// </summary>
        /// <returns>筆跡是否有效</returns>
        public bool IsValid()
        {
            return StartPoint != EndPoint && Width > 0;
        }
    }

    /// <summary>
    /// 筆跡類型列舉
    /// </summary>
    public enum StrokeType
    {
        /// <summary>
        /// 線條
        /// </summary>
        Line,

        /// <summary>
        /// 點
        /// </summary>
        Point,

        /// <summary>
        /// 曲線
        /// </summary>
        Curve
    }

    /// <summary>
    /// 筆跡資料事件參數
    /// 用於網路資料接收事件
    /// </summary>
    public class StrokeDataEventArgs : EventArgs
    {
        /// <summary>
        /// 筆跡資料
        /// </summary>
        public StrokeData StrokeData { get; }

        /// <summary>
        /// 資料來源 IP 位址
        /// </summary>
        public string SourceIP { get; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="strokeData">筆跡資料</param>
        /// <param name="sourceIP">來源 IP</param>
        public StrokeDataEventArgs(StrokeData strokeData, string sourceIP = "")
        {
            StrokeData = strokeData;
            SourceIP = sourceIP;
        }
    }

    /// <summary>
    /// 連線狀態事件參數
    /// 用於網路連線狀態變更事件
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// 是否已連線
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 狀態訊息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 遠端 IP 位址
        /// </summary>
        public string RemoteIP { get; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="isConnected">連線狀態</param>
        /// <param name="message">狀態訊息</param>
        /// <param name="remoteIP">遠端 IP</param>
        public ConnectionStatusEventArgs(bool isConnected, string message, string remoteIP = "")
        {
            IsConnected = isConnected;
            Message = message;
            RemoteIP = remoteIP;
        }
    }
} 