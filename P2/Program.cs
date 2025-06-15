using System;
using System.Windows.Forms;

namespace P2PMessenger
{
    /// <summary>
    /// P2P 訊息傳遞系統主程式入口點
    /// 根據 EXP2 設計講義實現點對點筆跡同步功能
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 啟用視覺樣式和文字呈現
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 記錄應用程式啟動
            Console.WriteLine("P2P 訊息傳遞系統啟動中...");
            
            // 啟動主視窗
            Application.Run(new MainForm());
        }
    }
} 