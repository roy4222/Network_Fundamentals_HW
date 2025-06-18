# Exp3 設計講義 - 網路概論自主學習作業

## 1. 多人線上系統實現方式

### 問題
前二個範例的網路連線方式是「點對點 P2P」的模式。那如何實現「多人線上(Multiuser Online)」的系統呢？

### 解答：主從式 (Client-Server) 架構

將系統區分成：
- **【客戶端】(主 Client)**：負責使用者操作介面、畫面呈現、資料收發等
- **【伺服器端】(從 Server)**：負責提供服務、資料傳遞、保存資料等

## 2. TCP 主從式運作架構圖

### 系統架構說明

#### 伺服器端 (Server)
- IP: 168.150.1.100
- 功能：
  - **Sender**：負責發送資料到客戶端
  - **Listener**：監聽客戶端連線請求

#### 客戶端 (Client)
- **P1 客戶端**：IP 192.168.0.200
- **P2 客戶端**：IP 192.168.0.120

每個客戶端都包含：
- **Listener**：接收來自伺服器的資料
- **Sender**：發送資料到伺服器

### 運作流程

1. **連線建立**：
   - 伺服器會與想連線的客戶端建立一個連線來專門服務它
   - 伺服器會維護一個連線位置的表來進行資料傳遞

2. **資料傳遞**：
   - 客戶端彼此是不直接連線的
   - 但是可以經由伺服器【轉傳】達到私訊的功能

### 關鍵特點

- **A.** 伺服器會與想連線的客戶端建立一個連線來專門服務它，伺服器會維護一個連線位置的表來進行資料傳遞
- **B.** 客戶端彼此是不直接連線的，但是可以經由伺服器【轉傳】達到私訊的功能

## 技術架構優勢

1. **集中管理**：所有資料和連線透過伺服器集中處理
2. **擴展性**：可以支援多個客戶端同時連線
3. **資料一致性**：透過伺服器確保資料同步
4. **安全性**：客戶端間不直接通訊，降低安全風險

---

## 3. 實作流程：Server端與Client端程式撰寫

### 3.1 Server端程式撰寫

#### 目標
建立一個Server端程式，檢查帳號（ID）是否存在於檔案中，並回應Client端的登入請求。

#### 檔案操作
- **檔案位置**：在應用程式目錄中建立 `System.txt` 用於儲存帳號資料
- **檔案格式**：每行一筆資料，目前僅儲存帳號（ID），未來需加入密碼欄位
- **使用命名空間**：`System.IO` 處理檔案操作

#### 檔案檢查與讀取
- 使用 `File.Exists` 確認 `System.txt` 是否存在
- 使用 `Path.Combine` 組合根目錄與檔案名稱
- 使用 `StreamReader` 逐行讀取檔案內容
- 每行讀取後與輸入的ID進行比對
- 如果ID匹配，設置回傳值 `RV = true` 並跳出迴圈（`break`）

#### 邏輯流程
1. 初始化 `RV = false` 作為預設回傳值
2. 如果檔案存在，逐行讀取並比對ID
3. 如果ID匹配，設置 `RV = true` 並終止讀取
4. 如果檔案不存在或無匹配ID，回傳 `RV = false`
5. 將結果回傳給Client端：
   - 若 `RV = true`，表示帳號存在，允許登入
   - 若 `RV = false`，表示無此帳號，拒絕登入並回傳狀態碼（如 `2`）

#### 程式碼範例（Server端檢查ID）
```csharp
using System.IO;

bool CheckID(string id)
{
    bool RV = false;
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.txt");

    if (File.Exists(path))
    {
        using (StreamReader sr = new StreamReader(path))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == id)
                {
                    RV = true;
                    break;
                }
            }
        }
    }
    return RV;
}
```

#### Server端回應
- 若找到ID，傳送成功訊號（如狀態碼 `1`）給Client端
- 若未找到ID，傳送失敗訊號（如狀態碼 `2`）

### 3.2 Client端程式撰寫

#### 目標
發送帳號（ID）給Server端，接收回應並顯示登入結果。

#### 流程
1. Client端發送帳號給Server端
2. 監聽Server端的回應訊號：
   - 若收到狀態碼 `1`，顯示登入成功
   - 若收到狀態碼 `2`，顯示「伺服器拒絕登入」
3. 若未收到回應或連線失敗，顯示錯誤訊息（如「伺服器斷線」）

#### 問題與解決
- 確保Server端有正確傳送回應訊號（狀態碼）
- Client端的監聽（`Listen`）邏輯需正確處理回應，避免未收到訊號的情況
- 避免隨意關閉 `Socket`（`SK`）或線程（`Thread`），否則可能導致錯誤

### 3.3 加強功能：帳號與密碼驗證

#### 需求
- 檔案 `System.txt` 需擴展格式，包含帳號與密碼（例如：`ID,Password`）
- Server端需檢查帳號與密碼是否同時匹配
- Client端需提供輸入帳號與密碼的欄位
- 拒絕登入的情況：
  - 帳號不存在
  - 帳號存在但密碼錯誤
- 回應訊息統一為「伺服器拒絕登入」，不需明確告知原因

#### 檔案格式範例（System.txt）
```
user1,password123
user2,password456
```

#### 程式碼範例（Server端檢查帳號與密碼）
```csharp
using System.IO;

bool CheckCredentials(string id, string password)
{
    bool RV = false;
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.txt");

    if (File.Exists(path))
    {
        using (StreamReader sr = new StreamReader(path))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 2 && parts[0] == id && parts[1] == password)
                {
                    RV = true;
                    break;
                }
            }
        }
    }
    return RV;
}
```

#### Client端改進
- 增加密碼輸入欄位
- 將帳號與密碼一起發送給Server端（例如：`ID:Password` 格式）
- 接收Server端的回應，顯示登入成功或失敗

## 4. 技術注意事項

### 4.1 多線程（Thread）管理
- 使用多線程處理Client端的連線，避免阻塞主程式
- 避免使用 `Thread.Abort`（不推薦，可能導致未預期的行為）
- 若需終止線程，考慮使用 `Thread.Interrupt` 或設計旗標（如 `CancellationToken`）來安全終止

### 4.2 Socket管理
- 不要隨意關閉 `Socket`（`SK`），否則可能導致連線錯誤
- 確保Server端與Client端的連線穩定，妥善處理異常（如斷線）

### 4.3 網路協議選擇
- **TCP**：適合需要穩定連線的應用（如登入系統）
- **UDP**：適合快速但不保證傳輸的場景

### 4.4 錯誤處理
- Server端應檢查檔案是否存在，避免檔案未找到的異常
- Client端應處理連線失敗或伺服器無回應的情況，顯示適當的錯誤訊息

## 5. 作業要求與學習目標

### 5.1 作業內容
- 完成三個範例的加強版程式（Server端與Client端）
- 實現帳號與密碼的驗證功能
- 上傳程式碼至指定平台（如 chunkcast）

### 5.2 加分功能
- 自行設計並實現額外功能（例如多人同時登入、聊天功能等）
- 提交功能設計的討論或說明，展示創意

### 5.3 學習目標
- 掌握網路應用程式的核心概念（Client-Server架構、檔案操作、多線程）
- 熟悉 TCP/UDP 協議的使用
- 提升程式除錯與錯誤處理能力

## 6. 總結

### Server端重點
- 建立 `System.txt` 儲存帳號與密碼
- 檢查檔案是否存在，逐行讀取並比對帳號密碼
- 回傳結果給Client端

### Client端重點
- 發送帳號與密碼
- 接收Server端回應，顯示登入結果

### 關鍵注意事項
- 避免關閉 `Socket` 或 `Thread`
- 使用多線程處理連線
- 妥善處理異常情況
- 完成程式並上傳，設計額外功能並提交說明
