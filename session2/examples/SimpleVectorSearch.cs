using System;
using System.Collections.Generic;
using System.Linq;

/*
 * 安裝套件說明：
 * 本範例為觀念展示，模擬了將文字轉為向量（Embedding）以計算相似度的邏輯，
 * 並未使用真實的 Vector Database 來降低學習門檻。
 * 僅需內建 .NET 即可執行 (dotnet run)。
 * 
 * 若日後要實作真實向量引擎，建議可安裝：
 * dotnet add package Microsoft.SemanticKernel
 * 或使用 Qdrant, Milvus 等開源 Vector DB 之 C# SDK。
 */

namespace Session2Examples
{
    class SimpleVectorSearch
    {
        // 模擬的知識庫資料結構
        public class Document
        {
            public int Id { get; set; }
            public string Content { get; set; } = "";
            public double[] Vector { get; set; } = Array.Empty<double>();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== RAG 基礎：向量相似度搜尋模擬 ===");

            // 1. 模擬的兩筆機台維修「知識」以及其轉換好的向量（在此為簡化後的一維數值表示）
            // 在實務中，這些 double 陣列會有數千維度，並由 Embedding 模型產生
            var knowledgeBase = new List<Document>
            {
                new Document { Id = 1, Content = "冷卻系統水壓不足，導致溫度過高報警。", Vector = new double[] { 0.8, 0.2, 0.1 } },
                new Document { Id = 2, Content = "傳送帶馬達皮帶斷裂，導致進料停滯。", Vector = new double[] { 0.1, 0.9, 0.2 } },
                new Document { Id = 3, Content = "系統記憶體不足，軟體無預警崩潰。", Vector = new double[] { 0.3, 0.1, 0.9 } }
            };

            // 2. 模擬使用者輸入問題，並被「轉換」成問題向量
            Console.WriteLine("\n使用者問：機器變好熱，是不是水管塞住了？");
            // 由於問題探討的是溫度與水，其向量特徵與第一筆知識會比較接近
            double[] questionVector = new double[] { 0.75, 0.15, 0.1 };

            Console.WriteLine("\n[系統] 開始計算向量相似度 (Cos 余弦相似度)...");

            // 3. 遍歷知識庫，計算相似度並找出最符合的段落
            var results = knowledgeBase.Select(doc => new
            {
                Document = doc,
                Similarity = CosineSimilarity(questionVector, doc.Vector)
            })
            .OrderByDescending(x => x.Similarity)   // 這邊會依照相似度由高到低排序
            .ToList();

            // 輸出結果
            Console.WriteLine("\n檢索結果排名：");
            foreach (var res in results)
            {
                Console.WriteLine($"相符程度: {res.Similarity:P2} | 知識庫內容: {res.Document.Content}");
            }

            Console.WriteLine("\n[系統] 接著，會將相符程度最高的那筆資料，夾帶到 Prompt 給 LLM 產生最終回覆。");

            Console.WriteLine("\n相似度最高的知識庫內容是：");
            var maxResult = results[0];
            Console.WriteLine(maxResult.Document.Content);

            // 這邊就會接上往 ollama or 其他 LLM server 送的 code segment
        }

        // 輔助方法：計算餘弦相似度 (Cosine Similarity)
        // 這是最常用來比較兩個向量語意接近程度的數學算法
        static double CosineSimilarity(double[] vector1, double[] vector2)
        {
            double dotProduct = 0;
            double norm1 = 0;
            double norm2 = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                norm1 += vector1[i] * vector1[i];
                norm2 += vector2[i] * vector2[i];
            }
            if (norm1 == 0 || norm2 == 0) return 0;
            return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }
    }
}
