using System;
using System.Windows.Forms;

namespace P2PMessenger
{
    /// <summary>
    /// 應用程式的主要入口點
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 配置應用程式的視覺樣式
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Console.WriteLine("=== UDP P2P 訊息應用程式啟動 ===");
                Console.WriteLine("版本: 1.0.0");
                Console.WriteLine("開發階段: 里程碑 1 - 基礎UDP通訊框架");
                Console.WriteLine("========================================");

                // 啟動主表單
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"應用程式啟動時發生嚴重錯誤: {ex.Message}");
                MessageBox.Show(
                    $"應用程式啟動失敗:\n{ex.Message}", 
                    "嚴重錯誤", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                Console.WriteLine("=== 應用程式結束 ===");
            }
        }
    }
} 