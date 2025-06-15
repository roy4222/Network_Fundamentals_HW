# UDP P2P 文字訊息應用程式(完成)



## 🚀 快速開始

### 系統需求
- ✅ **.NET 9.0 SDK** (您的系統：v9.0.300 - Windows x64)
- Windows 10/11
- Visual Studio 2022/2025 (可選)

### 運行步驟
```bash
# 1. 進入專案目錄
cd P1

# 2. 編譯專案
dotnet build

# 3. 運行應用程式
dotnet run
```

### P2P通訊測試
1. **開啟兩個應用程式實例**
2. **配置連線設定**：
   - 第一個實例：本地端口8001，目標端口8002，IP: 127.0.0.1，暱稱: 保羅
   - 第二個實例：本地端口8002，目標端口8001，IP: 127.0.0.1，暱稱: 瑪麗
3. **開始測試**：
   - 在兩個實例中點擊「開始監聽」
   - 設定各自的暱稱（如：保羅、瑪麗）
   - 輸入訊息並發送，確認另一個實例能收到格式化的暱稱訊息

---

## 📋 專案概述

### 核心功能
本專案實現了一個基於UDP協定的P2P（點對點）文字訊息應用程式：

- **雙向通訊**：兩個應用程式實例互相發送和接收文字訊息
- **UDP協定**：使用UDP套接字進行網路通訊
- **P2P架構**：每個實例既是發送者也是監聽者
- **即時訊息**：支援即時文字訊息傳送
- **Windows Forms UI**：提供友善的圖形化操作介面

### 技術特色
- ✅ **多線程設計**：解決Windows Forms UI阻塞問題
- ✅ **UTF-8編碼**：完整支援中文訊息
- ✅ **事件驅動架構**：低耦合、高可維護性
- ✅ **資源管理**：適當的IDisposable實現
- ✅ **錯誤處理**：完善的異常處理和日誌記錄

---

## 🏗 架構設計

### 核心類別
```
📁 P2PMessenger
├── 🔧 UdpSender.cs        - UDP訊息發送器
├── 📡 UdpListener.cs       - UDP訊息監聽器 (多線程)
├── 🖥️ MainForm.cs         - Windows Forms主介面
├── ⚙️ Program.cs          - 應用程式入口點
└── 🧪 TestUdpBasic.cs     - 自動化測試工具
```

### 多線程架構
```csharp
// 主UI線程 (Windows Forms)
MainForm → UI控制和事件處理

// 獨立監聽線程 (避免UI阻塞)
UdpListener → Task.Run(() => ListenForMessagesAsync())

// 線程安全UI更新
if (this.InvokeRequired)
    this.Invoke(new Action(() => UpdateUI()));
```

### 通訊流程
```
1. 應用程式啟動 → 初始化UDP組件
2. 使用者點擊「開始監聽」→ 啟動背景監聽線程
3. 使用者輸入訊息 → UDP發送器傳送到目標
4. 背景線程接收訊息 → 觸發事件更新UI
5. 使用者關閉程式 → 優雅停止所有線程
```

---

## 🔧 增強版功能需求

### 核心問題解決
本專案解決了Windows Forms應用程式中常見的**子線程關閉問題**：

#### 問題背景
- **主要問題**：應用程式關閉時，UDP監聽子線程未正確關閉，導致系統卡住或異常
- **原因分析**：預設元件行為或線程未正確終止，導致系統無法完全關閉

#### 解決方案實現
1. **事件註冊機制**
   - 監聽 `FormClosing` 事件，確保在視窗關閉時觸發相關處理
   - 在事件中加入程式碼，確保所有系統資源（包括線程）正確關閉

2. **Try-Catch 結構**
   - 將可能出錯的程式碼放入 try 區塊，避免系統因異常崩潰
   - 確保異常被捕獲，提供友善的錯誤處理

3. **資源關閉流程**
   - 使用 `CancellationToken` 優雅地終止監聽線程
   - 針對 `UdpClient` 執行 `Close()` 和 `Dispose()`，確保資源被釋放
   - 實現完整的 `IDisposable` 模式

### 功能增強特色

#### 🏷️ 暱稱系統
- **需求**：在訊息傳輸中加入「暱稱（NickName）」欄位，標示訊息發送者
- **效果**：顯示格式如「保羅說：你好！」或「瑪麗說：收到了」
- **支援**：多人通訊場景，透過暱稱區分不同發送者

#### 實現方式
1. **介面增強**
   - 新增「暱稱」輸入欄位，讓使用者設定自己的暱稱
   - 發送訊息時，將暱稱附加到訊息內容（格式：`暱稱: 訊息`）

2. **訊息解析**
   - 接收端解析訊息，提取發送者的暱稱及內容
   - 在接收區域以友善格式顯示：`[時間] 暱稱說：訊息內容`

3. **多方通訊支援**
   - 透過約定相同的通訊協定實現多方通訊
   - 支援基於IP的遠端資料傳輸，實現簡單且高效的通訊系統

#### 技術優勢
- ✅ **優雅關閉**：解決子線程殭屍問題，確保系統完全關閉
- ✅ **身份識別**：暱稱系統讓訊息更具可讀性
- ✅ **多人支援**：支持多人互動，提升系統可用性
- ✅ **錯誤處理**：完善的異常處理機制，提升系統穩定性

---

## 🎯 開發里程碑

### ✅ 里程碑1：基礎UDP通訊框架 (已完成)
**核心成就**：
- UDP發送器和監聽器類別
- Windows Forms使用者介面
- 多線程非同步處理
- 事件驅動架構
- 完整的錯誤處理

### ✅ 里程碑2：暱稱系統功能 (已完成)
**核心成就**：
- ✅ 新增暱稱輸入欄位，讓使用者設定個人暱稱
- ✅ 訊息發送格式：「暱稱: 訊息內容」
- ✅ 訊息接收顯示：「[時間] 暱稱說：訊息內容」
- ✅ 支援多人通訊場景，透過暱稱區分不同發送者
- ✅ 向下相容：支援無暱稱格式的舊版訊息

### 🔄 里程碑3：雙向P2P通訊優化 (下一步)
**計劃目標**：
- 連線狀態管理
- 自動重連機制
- 訊息確認機制
- 心跳檢測功能

### 🎨 里程碑4：使用者介面改善
**計劃目標**：
- 更美觀的UI設計
- 訊息歷史記錄
- 聊天室風格介面
- 系統托盤支援

### ⚙️ 里程碑5：網路配置管理
**計劃目標**：
- 配置檔案系統
- 網路檢測功能
- 防火牆指引
- 多網路介面支援

### 🚀 里程碑6：進階功能
**計劃目標**：
- 檔案傳送功能
- 加密通訊
- 群組聊天
- 性能最佳化

---

## 🛠 技術規格

### 開發環境
- **語言**：C# (.NET 9.0)
- **UI框架**：Windows Forms
- **網路庫**：System.Net.Sockets.UdpClient
- **並發**：Task-based Asynchronous Pattern (TAP)

### 程式碼品質
- **設計模式**：事件驅動、觀察者模式
- **SOLID原則**：單一職責、開放封閉、依賴反轉
- **文檔標準**：Google Style docstring
- **錯誤處理**：結構化異常處理

### 網路協定
- **傳輸層**：UDP (User Datagram Protocol)
- **編碼**：UTF-8 (支援中文)
- **端口範圍**：8001-8999 (可自定義)
- **測試環境**：本地環路 (127.0.0.1)

---

## 🐛 問題排除

### 常見問題
1. **防火牆阻擋**
   - 現象：訊息無法接收
   - 解決：允許應用程式通過Windows防火牆

2. **端口被占用**
   - 現象：監聽啟動失敗
   - 解決：更改本地監聽端口設定

3. **編譯錯誤**
   - 現象：dotnet build失敗
   - 解決：確認.NET 9.0 SDK已正確安裝

4. **找不到專案**
   - 現象：`找不到要執行的專案`
   - 解決：確保在P1目錄中執行`dotnet run`

### 偵錯技巧
- 檢查Console輸出的日誌訊息
- 確認防火牆設定
- 使用不同端口測試
- 檢查IP地址設定是否正確

---

## 📚 技術文檔

### 核心API
```csharp
// UDP發送器
public class UdpSender : IDisposable
{
    public Task<bool> SendMessageAsync(string message, string ip, int port);
}

// UDP監聽器
public class UdpListener : IDisposable
{
    public event Action<string, string, int> MessageReceived;
    public Task<bool> StartListeningAsync();
    public Task StopListeningAsync();
}
```

### 事件處理
```csharp
// 訊息接收事件
udpListener.MessageReceived += (message, senderIp, senderPort) =>
{
    Console.WriteLine($"收到訊息: {message} 來自 {senderIp}:{senderPort}");
};

// 線程安全UI更新
if (this.InvokeRequired)
    this.Invoke(new Action(() => AppendMessage(message)));
```

### 🔍 傳送事件機制
```csharp
// 按鈕點擊觸發傳送 (MainForm.cs:279)
private async void BtnSendMessage_Click(object? sender, EventArgs e)
    → 直接調用 SendMessageAsync()

// Enter鍵觸發傳送 (MainForm.cs:287)  
private async void TxtMessage_KeyPress(object? sender, KeyPressEventArgs e)
    → 檢查Enter鍵後調用 SendMessageAsync()

// 核心傳送邏輯 (MainForm.cs:297)
private async Task SendMessageAsync()
    → 組合暱稱格式 → UdpSender.SendMessageAsync() → UI更新
```

### 🏷️ 暱稱系統機制
```csharp
// 訊息發送格式化 (MainForm.cs:320)
string formattedMessage = $"{nickname}: {message}";
    → 發送格式：「保羅: 你好！」

// 訊息接收解析 (MainForm.cs:365)
if (message.Contains(": "))
{
    string nickname = message.Substring(0, separatorIndex);
    string content = message.Substring(separatorIndex + 2);
    displayMessage = $"{nickname}說：{content}";
}
    → 顯示格式：「保羅說：你好！」

// 向下相容處理
else → displayMessage = $"匿名用戶說：{message}";
```

### 🌐 IP位置取得機制
| 取得方式 | 程式碼位置 | 用途 | 來源 |
|---------|-----------|------|------|
| **接收方IP** | `UdpListener.cs:136`<br/>`result.RemoteEndPoint.Address` | 自動偵測發送方IP | UDP封包標頭 |
| **目標IP輸入** | `MainForm.cs:313`<br/>`txtRemoteIp.Text.Trim()` | 發送訊息目標 | 使用者輸入 |
| **IP解析** | `UdpSender.cs:55`<br/>`IPAddress.Parse(remoteIp)` | 字串轉IP物件 | 參數傳入 |
| **預設IP** | `MainForm.cs:85`<br/>`Text = "127.0.0.1"` | UI初始值 | 程式設定 |
| **測試IP** | `TestUdpBasic.cs:38`<br/>`testIp = "127.0.0.1"` | 自動化測試 | 硬編碼 |

**特別注意**：目前程式未實現本機IP自動偵測、DNS解析或網路介面卡列舉功能。

---

## 📄 授權與版權

**專案性質**：教育用途 - 網路概論自主學習作業  
**開發日期**：2025年6月  
**技術支援**：基於.NET 9.0和Windows Forms  
**授權條款**：學術使用授權
