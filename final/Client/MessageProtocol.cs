using System;

namespace ChatClient
{
    /// <summary>
    /// 定義客戶端與伺服器之間的通訊協定
    /// 使用簡單的字串格式：MESSAGE_TYPE:content
    /// </summary>
    public static class MessageProtocol
    {
        // 訊息類型常數
        public const string LOGIN = "LOGIN";
        public const string LOGOUT = "LOGOUT";
        public const string BROADCAST = "BROADCAST";
        public const string PRIVATE = "PRIVATE";
        public const string USER_LIST = "USER_LIST";
        public const string FILE_REQUEST = "FILE_REQUEST";
        public const string FILE_RESPONSE = "FILE_RESPONSE";
        public const string ERROR = "ERROR";
        public const string SUCCESS = "SUCCESS";
        public const string SYSTEM_NOTIFICATION = "SYSTEM_NOTIFICATION";
        public const string USER_JOINED = "USER_JOINED";
        public const string USER_LEFT = "USER_LEFT";

        // 分隔符號
        public const char SEPARATOR = ':';

        /// <summary>
        /// 建立登入訊息
        /// 格式：LOGIN:username
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <returns>格式化的登入訊息</returns>
        public static string CreateLoginMessage(string username)
        {
            return $"{LOGIN}{SEPARATOR}{username}";
        }

        /// <summary>
        /// 建立登出訊息
        /// 格式：LOGOUT:username
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <returns>格式化的登出訊息</returns>
        public static string CreateLogoutMessage(string username)
        {
            return $"{LOGOUT}{SEPARATOR}{username}";
        }

        /// <summary>
        /// 建立廣播訊息 (客戶端發送用)
        /// 格式：BROADCAST:message_content
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的廣播訊息</returns>
        public static string CreateBroadcastMessage(string message)
        {
            return $"{BROADCAST}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 建立私人訊息 (客戶端發送用)
        /// 格式：PRIVATE:target_username:message_content
        /// </summary>
        /// <param name="targetUsername">目標使用者名稱</param>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的私人訊息</returns>
        public static string CreatePrivateMessage(string targetUsername, string message)
        {
            return $"{PRIVATE}{SEPARATOR}{targetUsername}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 解析收到的訊息
        /// </summary>
        /// <param name="message">原始訊息</param>
        /// <returns>訊息類型和內容的元組</returns>
        public static (string messageType, string[] parts) ParseMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return (ERROR, new[] { "空白訊息" });
            }

            // 只在第一個分隔符號處分割，以區分訊息類型和內容
            // 伺服器發送的內容已預先格式化，可能包含冒號
            string[] parts = message.Split(new[] { SEPARATOR }, 2, StringSplitOptions.None);

            string messageType;
            string[] content;

            if (parts.Length == 1)
            {
                // 如果沒有分隔符，整個訊息就是類型，沒有內容
                messageType = parts[0];
                content = Array.Empty<string>();
            }
            else // parts.Length is 2
            {
                // 內容是分隔符號之後的所有部分
                messageType = parts[0];
                content = new[] { parts[1] };
            }

            Console.WriteLine($"[客戶端] 解析訊息 - 類型: {messageType}, 內容: {string.Join(", ", content)}");
            return (messageType, content);
        }

        /// <summary>
        /// 格式化顯示廣播訊息
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="username">發送者用戶名</param>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的顯示文字</returns>
        public static string FormatBroadcastDisplay(string timestamp, string username, string message)
        {
            return $"[{timestamp}] {username}: {message}";
        }

        /// <summary>
        /// 格式化顯示私人訊息
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="senderUsername">發送者用戶名</param>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的顯示文字</returns>
        public static string FormatPrivateDisplay(string timestamp, string senderUsername, string message)
        {
            return $"[{timestamp}] {senderUsername} (私訊): {message}";
        }

        /// <summary>
        /// 格式化顯示系統通知
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="message">通知訊息</param>
        /// <returns>格式化的顯示文字</returns>
        public static string FormatSystemNotificationDisplay(string timestamp, string message)
        {
            return $"[{timestamp}] 系統: {message}";
        }

        /// <summary>
        /// 格式化使用者加入通知
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="username">加入的使用者名稱</param>
        /// <returns>格式化的顯示文字</returns>
        public static string FormatUserJoinedDisplay(string timestamp, string username)
        {
            return $"[{timestamp}] 系統: {username} 已加入聊天室";
        }

        /// <summary>
        /// 格式化使用者離開通知
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="username">離開的使用者名稱</param>
        /// <returns>格式化的顯示文字</returns>
        public static string FormatUserLeftDisplay(string timestamp, string username)
        {
            return $"[{timestamp}] 系統: {username} 已離開聊天室";
        }
    }
} 