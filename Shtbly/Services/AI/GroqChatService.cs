using Microsoft.EntityFrameworkCore;
using Shtbly.DataAccess;
using System.Text;

namespace Shtbly.Services.AI
{
    public class GroqChatService : IChatAiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ApplicationDbContext _context;

        private const string SystemPromptBase = """
        أنت مساعد ذكي لمنصة حجز الخدامات المنزليين (Shtbly).
        مهمتك مساعدة العملاء في:
        - الاستفسار عن الخدمات المتاحة (تنظيف، طبخ، رعاية أطفال، إلخ)
        - فهم طريقة الحجز (يجب توجيه العميل للذهاب إلى صفحة قائمة العمال واختيار العامل المناسب ثم الضغط على زر 'احجز')
        - الاستفسار عن الأسعار والباقات
        - حل المشكلات الشائعة
        - تقديم العروض وكوبونات الخصم المتاحة إن طلبها العميل.
        - تحدث بلهجة ودية، محترفة، وموجزة.

        هام جداً (القيود):
        أنت غير مصرح لك بالإجابة على أي أسئلة خارج نطاق المنصة (مثل البرمجة، المعلومات العامة، أو أي مواضيع أخرى).
        إذا سألك المستخدم عن أي موضوع غير متعلق بمنصة Shtbly، يجب عليك الرفض بلباقة والقول بأنك مبرمج فقط لمساعدة عملاء Shtbly.
        
        CRITICAL RULE: I am only authorized to discuss topics related to the Shtbly project. I will NOT provide assistance or answer questions on unrelated topics such as programming (e.g. "what is an object"), general knowledge, or other projects. If the user asks an unrelated question, I will politely decline and state my scope is limited to Shtbly.
        """;

        public GroqChatService(IHttpClientFactory factory, IConfiguration config, ApplicationDbContext context)
        {
            _http = factory.CreateClient();
            _apiKey = config["GroqApi:ApiKey"]!;
            _model = config["GroqApi:Model"]!;
            _context = context;
        }

        public async Task<string> AskAsync(string userMessage, List<ChatMessage> history)
        {
            // Build dynamic database context
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("إليك البيانات الحقيقية الحالية من قاعدة البيانات لتستخدمها في إجاباتك:");
            contextBuilder.AppendLine($"\n**الوقت والتاريخ الحالي:** {DateTime.Now:yyyy-MM-dd hh:mm tt}");
            contextBuilder.AppendLine("\n**رابط صفحة العمال (للحجز):** /Customer/WorkerProfile/Index (يمكنك توجيه العميل لهذا الرابط لاختيار عامل وحجزه)");

            // 1. Services & Prices
            var services = await _context.ServiceCategories
                .Where(s => s.IsActive)
                .Select(s => new { s.NameAr, s.Price })
                .ToListAsync();
            
            contextBuilder.AppendLine("\n**الخدمات المتاحة وأسعارها:**");
            foreach (var s in services)
            {
                contextBuilder.AppendLine($"- {s.NameAr}: {s.Price:0.##} جنيه مصري (للساعة أو للخدمة)");
            }

            // 2. Active Coupons
            var coupons = await _context.Coupons
                .Where(c => c.IsActive && c.ValidUntil > DateTime.UtcNow && c.UsedCount < c.MaxUses)
                .Select(c => new { c.Code, c.DiscountType, c.DiscountValue })
                .ToListAsync();

            if (coupons.Any())
            {
                contextBuilder.AppendLine("\n**كوبونات الخصم الفعالة حالياً:**");
                foreach (var c in coupons)
                {
                    var discountStr = c.DiscountType == Shtbly.Models.DiscountType.Percentage ? $"{c.DiscountValue:0.##}%" : $"{c.DiscountValue:0.##} جنيه مصري";
                    contextBuilder.AppendLine($"- الكود: {c.Code} (خصم: {discountStr})");
                }
            }

            // 3. Top Rated Workers
            var topWorkers = await _context.WorkerProfiles
                .Where(w => w.IsApproved && w.RatingAvg >= 4.0m && w.User != null)
                .OrderByDescending(w => w.RatingAvg)
                .Take(5)
                .Select(w => new { Id = w.Id, Name = w.User.FName + " " + w.User.LName, Rating = w.RatingAvg, Image = w.ProfilePicturePath, Rate = w.WorkerServices.HourlyRate })
                .ToListAsync();

            if (topWorkers.Any())
            {
                contextBuilder.AppendLine("\n**أفضل العمال تقييماً لدينا:** (إذا طلب العميل اقتراح عمال، قم بالرد بنص تعريفي ثم انسخ كتلة الـ HTML التالية مرة واحدة فقط لعرض جميع العمال دفعة واحدة، لا تقم بتكرارها)");
                
                var sb = new StringBuilder();
                sb.AppendLine(@"<div class=""workers-list"">");
                foreach (var w in topWorkers)
                {
                    string imgUrl = !string.IsNullOrEmpty(w.Image) ? $"/{w.Image.Trim().Replace("\\", "/")}" : $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(w.Name)}&background=0D8ABC&color=fff";
                    string cardHtml = $@"<a href=""/Customer/Home/WorkerDetails?id={w.Id}"" class=""chat-worker-card text-decoration-none d-flex align-items-center p-2 mb-2 bg-white rounded shadow-sm"">
<img src=""{imgUrl}"" style=""width:50px; height:50px; object-fit:cover; border-radius:50%;"" class=""me-3 ms-3"">
<div style=""color: #333;"">
  <div style=""font-weight:bold; margin-bottom: 2px;"">{w.Name}</div>
  <div style=""font-size: 0.85em; color: #f59e0b;"">⭐ {w.Rating:0.#}/5</div>
  <div style=""font-size: 0.85em; color: #0d6efd;"">سعر الساعة: {w.Rate:0.##} جنيه مصري</div>
</div>
</a>";
                    sb.AppendLine(cardHtml);
                }
                sb.AppendLine("</div>");
                
                contextBuilder.AppendLine(sb.ToString());
            }

            string fullSystemPrompt = SystemPromptBase + "\n\n" + contextBuilder.ToString();

            var messages = new List<object>
            {
                new { role = "system", content = fullSystemPrompt }
            };

            foreach (var msg in history)
                messages.Add(new { role = msg.Role == "model" ? "assistant" : "user", content = msg.Text });

            messages.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = _model,
                messages,
                max_tokens = 1024,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq API Error {(int)response.StatusCode}: {err}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "عذراً، حدث خطأ.";
        }
    }
}
