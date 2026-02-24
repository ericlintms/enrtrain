using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

/*
 * 執行步驟：
 * 1. 建立終端機專案: dotnet new console
 * 2. 替換 Program.cs 為此檔案。
 * 3. 執行程式: dotnet run 
 * 4. 程式將在 http://localhost:8080/ 監聽請求。
 * 5. 使用瀏覽器或 curl 造訪 http://localhost:8080/，您就會看到這支簡單程式回傳的結果。
 * 
 * 註：現代 .NET C# 主要都使用 ASP.NET Core 建立 Minimal API，
 * 此處為了降低學習門檻，採用最基礎底層的 HttpListener 來展示概念。
 */

namespace AppendixExamples
{
    class SimpleWebAPI
    {
        static async Task Main(string[] args)
        {
            // 建立 HTTP 監聽器
            using (HttpListener listener = new HttpListener())
            {
                // 設定我們要想監聽的本機通訊埠
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();
                Console.WriteLine("簡易 Web API 伺服器已啟動於：http://localhost:8080/");
                Console.WriteLine("請開啟瀏覽器前往該網址測試，按 Ctrl+C 結束程式...");

                while (true)
                {
                    // 非同步等待客戶端（如瀏覽器、您寫的其他程式）的請求
                    HttpListenerContext context = await listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;

                    Console.WriteLine($"\n[伺服器收到請求] 方法: {request.HttpMethod}, 網址: {request.Url}");

                    // 組合我們要回應給客戶端的內容 (JSON 格式)
                    string responseString = @"{ ""message"": ""歡迎來到您的第一個 API!"", ""status"": ""成功"" }";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                    // 取得回應物件，設定對應的 Header
                    HttpListenerResponse response = context.Response;
                    // 設定狀態碼 200 代表 OK
                    response.StatusCode = 200;
                    // 告訴客戶端我們回傳的是 JSON 格式，文字編碼是 UTF-8
                    response.ContentType = "application/json; charset=utf-8";
                    
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
        }
    }
}
