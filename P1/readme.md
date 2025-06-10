# UDP P2P 文字訊息應用程式

> **當前狀態**：✅ 里程碑1已完成 | ✅ .NET 9.0無警告編譯 | ✅ 應用程式運行中

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
   - 第一個實例：本地端口8001，目標端口8002，IP: 127.0.0.1
   - 第二個實例：本地端口8002，目標端口8001，IP: 127.0.0.1
3. **開始測試**：
   - 在兩個實例中點擊「開始監聽」
   - 輸入訊息並發送，確認另一個實例能收到

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

## 🎯 開發里程碑

### ✅ 里程碑1：基礎UDP通訊框架 (已完成)
**核心成就**：
- UDP發送器和監聽器類別
- Windows Forms使用者介面
- 多線程非同步處理
- 事件驅動架構
- 完整的錯誤處理

### 🔄 里程碑2：雙向P2P通訊優化 (下一步)
**計劃目標**：
- 訊息格式標準化
- 連線狀態管理
- 自動重連機制
- 訊息確認機制

### 🎨 里程碑3：使用者介面改善
**計劃目標**：
- 更美觀的UI設計
- 訊息歷史記錄
- 聊天室風格介面
- 系統托盤支援

### ⚙️ 里程碑4：網路配置管理
**計劃目標**：
- 配置檔案系統
- 網路檢測功能
- 防火牆指引
- 多網路介面支援

### 🚀 里程碑5：進階功能
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

---

## 📄 授權與版權

**專案性質**：教育用途 - 網路概論自主學習作業  
**開發日期**：2025年6月  
**技術支援**：基於.NET 9.0和Windows Forms  
**授權條款**：學術使用授權
