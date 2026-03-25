/*
 * SemanticKernelAgent.cs — Microsoft.SemanticKernel 完整示範
 * ============================================================
 *
 * 示範重點：
 *   1. 用 [KernelFunction] + [Description] 將 C# 方法宣告為 AI 工具
 *   2. 建立 Kernel 並透過 Ollama OpenAI-相容端點連接本地 LLM
 *   3. 使用 FunctionChoiceBehavior.Auto() 讓 SK 自動決定工具調用時機
 *   4. 維護 ChatHistory 進行多輪有記憶的對話
 *
 * 前置條件：
 *   1. 確認 Ollama 服務已啟動（預設 http://localhost:11434）
 *   2. 拉下支援 function calling 的模型：
 *        ollama pull llama3.2
 *
 * 安裝套件（已寫入 examples.csproj，首次執行會自動還原）：
 *   dotnet add package Microsoft.SemanticKernel
 *
 * 執行方式：
 *   cd session3/examples
 *   dotnet run
 * ============================================================
 */

#pragma warning disable SKEXP0010 // AddOpenAIChatCompletion with custom endpoint is experimental
#pragma warning disable SKEXP0001 // FunctionChoiceBehavior is experimental

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

// ── Plugin 定義 ──────────────────────────────────────────────────────
//
// [KernelFunction("識別名稱")]
//   → 告訴 SK 此方法可被 AI 呼叫，並給予 snake_case 的唯一識別名稱
//
// [Description("...")]（用在方法上）
//   → 自然語言描述「這個工具的用途」，LLM 依此決定要不要呼叫
//
// [Description("...")]（用在參數上）
//   → 自然語言描述「這個參數代表什麼」，協助 LLM 正確填入參數值
//
// 真實情境中，方法主體可替換為：
//   - 對 MariaDB / SQL Server 的查詢
//   - 呼叫 ERP REST API
//   - 讀取 SCADA 感測器數值

public class FactoryPlugin
{
    /// <summary>查詢機台維護保養紀錄</summary>
    [KernelFunction("check_maintenance")]
    [Description("查詢指定機台在某個月份的維護保養紀錄。當使用者詢問機台是否有保養、維修或異常時使用此工具。")]
    public string CheckMaintenance(
        [Description("機台編號，例如 E-01、E-02、E-03")] string machineId,
        [Description("查詢月份，格式為 YYYY-MM，例如 2024-01")] string month)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  [工具呼叫] CheckMaintenance(machineId: {machineId}, month: {month})");
        Console.ResetColor();

        // ↓ 在真實專案中替換為 DB 查詢或 API 呼叫
        return machineId switch
        {
            "E-03" => $"{month}：無定期保養紀錄，但 {month}-15 發現冷卻液過低，已緊急補充。",
            "E-01" => $"{month}：已完成月度保養，更換濾芯一組，狀態正常。",
            "E-02" => $"{month}：完成季度維護，更換密封墊片，目前運作正常。",
            _      => $"查無機台 {machineId} 在 {month} 的維護紀錄。請確認機台編號是否正確。"
        };
    }

    /// <summary>讀取即時感測器溫度</summary>
    [KernelFunction("get_temperature")]
    [Description("取得指定機台的即時感測器溫度讀數（攝氏）。當使用者詢問機台溫度、是否過熱或需要比較溫度時使用此工具。")]
    public string GetTemperature(
        [Description("機台編號，例如 E-01、E-02、E-03")] string machineId)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  [工具呼叫] GetTemperature(machineId: {machineId})");
        Console.ResetColor();

        // ↓ 在真實專案中替換為感測器 API 或 SCADA 即時數據
        return machineId switch
        {
            "E-01" => "機台 E-01 目前溫度 72°C，低於警戒值 90°C，狀態正常。",
            "E-02" => "機台 E-02 目前溫度 88°C，接近警戒值 90°C，建議持續觀察。",
            "E-03" => "機台 E-03 目前溫度 95°C，已超過警戒值 90°C！建議立即派員檢查冷卻系統。",
            _      => $"查無機台 {machineId} 的溫度感測資料，請確認機台編號或感測器連線狀態。"
        };
    }
}

// ── 主程式進入點 ────────────────────────────────────────────────────

/// <summary>Semantic Kernel Agent 示範主程式</summary>
public static class SemanticKernelAgentDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Microsoft.SemanticKernel Agent 示範 ===");
        Console.WriteLine("(請確認 Ollama 已啟動且已拉下 llama3.2 模型)\n");

        // ── Step 1：建立 Kernel ──────────────────────────────────────
        //
        // Kernel 是 SK 的核心容器，負責統一管理：
        //   - 連接的 LLM 服務
        //   - 已註冊的 Plugin / Function
        //   - Filter、Middleware（進階功能）

        var builder = Kernel.CreateBuilder();

        // 使用 Ollama 的 OpenAI 相容端點（/v1）
        // 這讓我們只需要 Microsoft.SemanticKernel 這一個套件，
        // 不需要額外安裝 Ollama 專用的連接器。
        //
        // 若要改用 Azure OpenAI，只需替換這行：
        //   builder.AddAzureOpenAIChatCompletion("部署名稱", "https://xxx.azure.com/", "API金鑰");
        builder.AddOpenAIChatCompletion(
            modelId:  "llama3.2",
            apiKey:   "ollama",                        // Ollama 不驗證 API Key
            endpoint: new Uri("http://localhost:11434/v1"));

        var kernel = builder.Build();

        // ── Step 2：將工具集掛入 Kernel ─────────────────────────────
        //
        // AddFromObject 掃描 FactoryPlugin 上所有 [KernelFunction] 方法，
        // 自動產生工具描述清單並在下次 LLM 呼叫時一起送出。
        // 第二個參數為此 Plugin 的命名空間（Plugin 名稱）。

        kernel.Plugins.AddFromObject(new FactoryPlugin(), "FactoryTools");

        Console.WriteLine("已載入工具：");
        foreach (var fn in kernel.Plugins.GetFunctionsMetadata())
        {
            Console.WriteLine($"  - {fn.PluginName}.{fn.Name}：{fn.Description}");
        }

        // ── Step 3：設定 Auto Function Calling ──────────────────────
        //
        // FunctionChoiceBehavior.Auto()：
        //   讓 SK + LLM 自行決定要不要呼叫工具、呼叫哪個工具、
        //   以及是否需要連續多次呼叫（Agentic Loop）。
        //   開發者完全不需要手動解析 JSON 或撰寫迴圈。
        //
        // 其他選項：
        //   FunctionChoiceBehavior.None()     → 完全禁止呼叫工具
        //   FunctionChoiceBehavior.Required() → 強制至少呼叫一次工具

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        // ── Step 4：建立對話歷程並開始多輪對話 ─────────────────────
        //
        // ChatHistory 儲存完整對話記憶：
        //   AddSystemMessage  → 設定 AI 角色與行為規範
        //   AddUserMessage    → 使用者訊息
        //   AddAssistantMessage → AI 回覆（加入後才有記憶延續效果）

        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(
            "你是一位工廠智能助手，負責協助工程師查詢機台資訊。\n" +
            "當需要即時資料時，請主動使用提供的工具取得，再整合成清楚的回答。\n" +
            "請一律使用繁體中文回答，並在必要時給出具體建議。");

        // 模擬三個不同情境的使用者問題
        string[] questions =
        [
            "機台 E-03 在 2024-01 有保養紀錄嗎？",
            "E-01 和 E-03 現在的溫度各是多少？哪台比較危險？",
            "綜合剛才查到的溫度和保養狀況，你對 E-03 有什麼建議？"
        ];

        foreach (var question in questions)
        {
            Console.WriteLine($"\n{new string('─', 55)}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"User  : {question}");
            Console.ResetColor();

            history.AddUserMessage(question);

            // GetChatMessageContentAsync：
            //   SK 在內部自動完成「推理 → 呼叫工具 → 整合工具結果 → 再推理 → 回覆」
            //   整個 Agentic Loop，直到 LLM 判斷不再需要呼叫工具為止。
            var response = await chatCompletion.GetChatMessageContentAsync(
                history, settings, kernel);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Agent : {response.Content}");
            Console.ResetColor();

            // 將 AI 回覆加回歷程，讓下一輪對話有上下文記憶
            history.AddAssistantMessage(response.Content ?? string.Empty);
        }

        Console.WriteLine($"\n{new string('─', 55)}");
        Console.WriteLine("示範結束。");
    }
}
