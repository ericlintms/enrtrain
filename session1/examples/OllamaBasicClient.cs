using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/*
 * 安裝套件說明：
 * 本範例僅使用 .NET 內建函式庫，不需要額外透過 NuGet 安裝套件。
 * 只要確保您的環境有安裝 .NET (6.0 或更高版本) 即可。
 * 
 * 執行步驟：
 * 1. 確保地端 Ollama 服務已啟動，預設在 http://localhost:11434。
 * 2. 確保有下載模型，終端機執行：ollama run llama3
 * 3. 建立終端機專案: dotnet new console
 * 4. 將現有 Program.cs 更換為此程式碼。
 * 5. 執行: dotnet run
 */

namespace Session1Examples
{
    class OllamaBasicClient
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("開始透過 C# 呼叫地端 Ollama API...");

            // 1. 設定 Ollama 服務的位址 (預設在本機 11434 port)
            string ollamaEndpoint = "http://10.1.58.1:11434/api/generate";

            // 2. 建立我們想傳遞給 AI 的請求物件
            // 這裡指定使用 llama3 模型，並提出我們遇到的工廠情境問題
            var requestData = new
            {
                model = "gemma3", // 需替換為您本地擁有的模型名稱
                prompt = "機台出現 ErrorCode: E-404, 感測器溫度異常。請給出排查建議。",
                stream = false    // 為了簡化範例，我們設定不以串流方式回傳，一次拿回完整結果
            };

            // 將請求物件序列化為 JSON 字串
            string jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // 3. 透過 HttpClient 發送 POST 請求
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // 若您的模型很大，回覆時間可能較長，實務上可調整 Timeout
                    client.Timeout = TimeSpan.FromMinutes(2);
                    
                    Console.WriteLine("\n[系統] 請求已發出，等待模型推論中...");
                    HttpResponseMessage response = await client.PostAsync(ollamaEndpoint, content);

                    // 確保 HTTP 狀態碼為 200 OK
                    response.EnsureSuccessStatusCode();

                    // 4. 讀取並解析回傳的 JSON 資料
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    
                    // 利用 JsonDocument 單純解析我們需要的 response 欄位
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        var root = doc.RootElement;
                        var resultText = root.GetProperty("response").GetString();
                        
                        Console.WriteLine("\n[AI 回覆]:\n" + resultText);
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\n[錯誤] 連線至 Ollama 失敗。請確認 Ollama 是否已啟動，以及位址是否正確。");
                    Console.WriteLine("例外訊息: " + e.Message);
                }
            }
        }
    }
}
