using System;

namespace ChatServer
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
        /// 建立廣播訊息 (含時間戳記)
        /// 格式：BROADCAST:timestamp:username:message_content
        /// </summary>
        /// <param name="username">發送者用戶名</param>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的廣播訊息</returns>
        public static string CreateBroadcastMessage(string username, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{BROADCAST}{SEPARATOR}{timestamp}{SEPARATOR}{username}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 建立廣播訊息 (客戶端發送用，不含時間戳記)
        /// 格式：BROADCAST:message_content
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的廣播訊息</returns>
        public static string CreateBroadcastMessage(string message)
        {
            return $"{BROADCAST}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 建立私人訊息 (含時間戳記)
        /// 格式：PRIVATE:timestamp:sender_username:target_username:message_content
        /// </summary>
        /// <param name="senderUsername">發送者用戶名</param>
        /// <param name="targetUsername">目標使用者名稱</param>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的私人訊息</returns>
        public static string CreatePrivateMessage(string senderUsername, string targetUsername, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{PRIVATE}{SEPARATOR}{timestamp}{SEPARATOR}{senderUsername}{SEPARATOR}{targetUsername}{SEPARATOR}{message}";
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
        /// 建立使用者列表訊息
        /// 格式：USER_LIST:user1,user2,user3
        /// </summary>
        /// <param name="usernames">使用者名稱列表</param>
        /// <returns>格式化的使用者列表訊息</returns>
        public static string CreateUserListMessage(string[] usernames)
        {
            return $"{USER_LIST}{SEPARATOR}{string.Join(",", usernames)}";
        }

        /// <summary>
        /// 建立系統通知訊息
        /// 格式：SYSTEM_NOTIFICATION:timestamp:message
        /// </summary>
        /// <param name="message">通知訊息</param>
        /// <returns>格式化的系統通知訊息</returns>
        public static string CreateSystemNotificationMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{SYSTEM_NOTIFICATION}{SEPARATOR}{timestamp}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 建立使用者加入通知
        /// 格式：USER_JOINED:timestamp:username
        /// </summary>
        /// <param name="username">加入的使用者名稱</param>
        /// <returns>格式化的使用者加入通知</returns>
        public static string CreateUserJoinedMessage(string username)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{USER_JOINED}{SEPARATOR}{timestamp}{SEPARATOR}{username}";
        }

        /// <summary>
        /// 建立使用者離開通知
        /// 格式：USER_LEFT:timestamp:username
        /// </summary>
        /// <param name="username">離開的使用者名稱</param>
        /// <returns>格式化的使用者離開通知</returns>
        public static string CreateUserLeftMessage(string username)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            return $"{USER_LEFT}{SEPARATOR}{timestamp}{SEPARATOR}{username}";
        }

        /// <summary>
        /// 建立錯誤訊息
        /// 格式：ERROR:error_message
        /// </summary>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <returns>格式化的錯誤訊息</returns>
        public static string CreateErrorMessage(string errorMessage)
        {
            return $"{ERROR}{SEPARATOR}{errorMessage}";
        }

        /// <summary>
        /// 建立成功訊息
        /// 格式：SUCCESS:success_message
        /// </summary>
        /// <param name="successMessage">成功訊息</param>
        /// <returns>格式化的成功訊息</returns>
        public static string CreateSuccessMessage(string successMessage)
        {
            return $"{SUCCESS}{SEPARATOR}{successMessage}";
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

            string[] parts = message.Split(SEPARATOR);
            if (parts.Length < 2)
            {
                return (ERROR, new[] { "訊息格式錯誤" });
            }

            string messageType = parts[0];
            string[] content = new string[parts.Length - 1];
            Array.Copy(parts, 1, content, 0, content.Length);

            Console.WriteLine($"[協定] 解析訊息 - 類型: {messageType}, 內容: {string.Join(", ", content)}");
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
    }
} 