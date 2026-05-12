namespace Shatbly.Services.AI
{
    public class GroqChatService : IChatAiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;

        private const string SystemPrompt = """
        أنت مساعد ذكي لمنصة حجز الخدامات المنزليين.
        مهمتك مساعدة العملاء في:
        - الاستفسار عن الخدمات المتاحة (تنظيف، طبخ، رعاية أطفال، إلخ)
        - فهم طريقة الحجز والمواعيد المتاحة
        - الاستفسار عن الأسعار والباقات
        - حل المشكلات الشائعة
        رد دايماً بشكل واضح ومفيد وودي.
        """;

        public GroqChatService(IHttpClientFactory factory, IConfiguration config)
        {
            _http = factory.CreateClient();
            _apiKey = config["GroqApi:ApiKey"]!;
            _model = config["GroqApi:Model"]!;
        }

        public async Task<string> AskAsync(string userMessage, List<ChatMessage> history)
        {
            var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt }
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
