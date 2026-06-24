using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shatbly.Services.AI
{
    public class IdValidationService : IIdValidationService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;

        public IdValidationService(IHttpClientFactory factory, IConfiguration config)
        {
            _http = factory.CreateClient();
            _apiKey = config["GroqApi:ApiKey"];
        }

        public async Task<(bool IsValid, string Reason)> ValidateIdCardAsync(IFormFile idCardFile)
        {
            if (idCardFile == null || idCardFile.Length == 0)
            {
                return (false, "ID Card photo file is empty or missing. / ملف صورة الهوية فارغ أو غير موجود.");
            }

            // Local check: Allowed file extensions
            var ext = Path.GetExtension(idCardFile.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(ext))
            {
                return (false, "Invalid image format. Allowed formats: JPG, JPEG, PNG, WEBP. / صيغة الصورة غير صالحة. الصيغ المسموحة: JPG, JPEG, PNG, WEBP.");
            }

            // Local check: Max file size (5MB)
            if (idCardFile.Length > 5 * 1024 * 1024)
            {
                return (false, "File size exceeds the limit of 5MB. / حجم الملف يتجاوز الحد المسموح به وهو 5 ميجابايت.");
            }

            // If API Key is not configured, bypass AI validation (mock mode)
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                System.Diagnostics.Debug.WriteLine("Groq ApiKey is not configured. Bypassing AI validation.");
                return (true, "ID card accepted (Bypassed AI validation due to missing configuration).");
            }

            try
            {
                // Read file bytes and encode to base64
                using var ms = new MemoryStream();
                await idCardFile.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                var base64String = Convert.ToBase64String(fileBytes);
                var mimeType = idCardFile.ContentType;
                var dataUrl = $"data:{mimeType};base64,{base64String}";

                // Build Groq Vision Request Body
                var requestBody = new
                {
                    model = "llama-3.2-11b-vision-preview",
                    messages = new object[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = "Analyze this image and determine if it is a valid government-issued identity card, passport, driver's license, or national ID card. Reply with EXACTLY one of the following words:\n'VALID' (if the image is a valid identity document photo)\n'INVALID' (if the image is NOT a valid identity document photo, or is just a blank photo, general image, etc.).\nDo not include any other text, punctuation, or explanations. Respond with just the single word."
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = dataUrl
                                    }
                                }
                            }
                        }
                    },
                    max_tokens = 10,
                    temperature = 0.1
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = JsonContent.Create(requestBody);

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Groq Vision API Error: {err}");
                    // On API error, we log and fall back to local validation passing to avoid blocking signup during API downtime
                    return (true, "ID card accepted (Failed to connect to AI validation service).");
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = result
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim().ToUpper() ?? "";

                if (content.Contains("VALID") && !content.Contains("INVALID"))
                {
                    return (true, "ID successfully validated by AI.");
                }
                else
                {
                    return (false, "The uploaded image does not appear to be a valid government identity card or passport. Please upload a clear photo of your ID. / الصورة المرفوعة لا تبدو كبطاقة هوية حكومية أو جواز سفر صالح. يرجى رفع صورة واضحة لهويتك.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ID validation: {ex.Message}");
                return (true, "ID card accepted (Exception during AI validation).");
            }
        }
    }
}
