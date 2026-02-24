using System;
using System.Text.Json;

/*
 * 安裝套件說明：
 * 本程式為概念性展示 (Pseudo-code 型態的架構示範)，不用安裝額外套件。
 * 實務開發 C# Agent 常使用 Microsoft.SemanticKernel (SK) 框架。
 * 安裝指令範例：dotnet add package Microsoft.SemanticKernel
 * 
 * 執行步驟：
 * 針對本範例，只需要 dotnet run 即可看到流程演示。
 */

namespace Session3Examples
{
    class ToolCallingAgent
    {
        // 這是我們準備給 AI 使用的「企業工具/Function」
        // 在 Semantic Kernel 等框架中，可以透過屬性(Attribute)將其註冊給 AI 知道
        static string QueryMaintenanceHistory(string machineId, string date)
        {
            Console.WriteLine($"\n[系統後端執行] 🔍 執行 SQL 查詢 -> MachineId: {machineId}, Date: {date}");
            // 模擬連接 MariaDB 查詢資料庫
            if (machineId == "E-03")
            {
                return "無保養紀錄，且發現上個月冷卻液曾經過低。";
            }
            return "設備維護狀況正常。";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== AI Agent 執行流程概念示範 ===");

            // 1. 使用者輸入對話
            string userMessage = "請問機台 E-03 上個月維修過嗎？";
            Console.WriteLine($"\nUser: {userMessage}");

            // 2. 模擬 LLM 解析使用者的對話意圖，判斷需要調用工具
            // 實務上 LLM 會回傳特定的 JSON，告知我們要呼叫哪個 Method 與帶入什麼參數
            Console.WriteLine("\n[Agent 思考中...] 發現自身無此資料，決定呼叫 QueryMaintenanceHistory 工具");
            
            // 模擬 LLM 要求呼叫工具時，傳來的參數 JSON
            string llmToolCallJson = @"{ ""function_name"": ""QueryMaintenanceHistory"", ""args"": { ""machineId"": ""E-03"", ""date"": ""last_month"" } }";
            
            using (JsonDocument doc = JsonDocument.Parse(llmToolCallJson))
            {
                var functionName = doc.RootElement.GetProperty("function_name").GetString();
                if (functionName == "QueryMaintenanceHistory")
                {
                    // 解析參數
                    var mId = doc.RootElement.GetProperty("args").GetProperty("machineId").GetString();
                    var mDate = doc.RootElement.GetProperty("args").GetProperty("date").GetString();

                    // 3. 由 C# 後端代替 Agent 真正去執行該工具函式
                    string toolResult = QueryMaintenanceHistory(mId, mDate);

                    Console.WriteLine($"\n[Agent 工具回傳結果] {toolResult}");

                    // 4. 下一步：將這段 toolResult 丟回給 LLM，讓 LLM 將資料組織為人類能讀懂的回答
                    Console.WriteLine("\n[Agent 最終回答產出中...]");
                    Console.WriteLine($"Agent 回復: 根據系統紀錄，機台 E-03 上個月並沒有保養紀錄；不過系統顯示曾在上個月發生過冷卻液過低的問題，或許與您目前的狀況有關。");
                }
            }
        }
    }
}
