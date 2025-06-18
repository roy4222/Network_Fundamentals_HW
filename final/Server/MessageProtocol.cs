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
        /// 建立廣播訊息
        /// 格式：BROADCAST:message_content
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <returns>格式化的廣播訊息</returns>
        public static string CreateBroadcastMessage(string message)
        {
            return $"{BROADCAST}{SEPARATOR}{message}";
        }

        /// <summary>
        /// 建立私人訊息
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
    }
} 