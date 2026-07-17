using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Stripe;

namespace Shtbly.HealthCheck
{
    public class ExternalServicesHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalServicesHealthCheck(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var reports = new List<string>();
            bool hasIssues = false;

            // 1. Check Stripe API
            var stripeSecretKey = _config["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(stripeSecretKey))
            {
                reports.Add("Stripe API key is not configured.");
                hasIssues = true;
            }
            else
            {
                try
                {
                    // Call lightweight API to verify configuration
                    var balanceService = new BalanceService();
                    var requestOptions = new RequestOptions { ApiKey = stripeSecretKey };
                    var balance = await balanceService.GetAsync(requestOptions, cancellationToken);
                    if (balance != null)
                    {
                        reports.Add("Stripe API is online and authenticated.");
                    }
                }
                catch (Exception ex)
                {
                    reports.Add($"Stripe connection check failed: {ex.Message}");
                    hasIssues = true;
                }
            }

            // 2. Check Groq AI API
            var groqApiKey = _config["GroqApi:ApiKey"];
            var groqModel = _config["GroqApi:Model"] ?? "llama-3.3-70b-versatile";
            if (string.IsNullOrWhiteSpace(groqApiKey))
            {
                reports.Add("Groq API key is not configured.");
                hasIssues = true;
            }
            else
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                    request.Headers.Add("Authorization", $"Bearer {groqApiKey}");
                    
                    var body = new
                    {
                        model = groqModel,
                        messages = new[] { new { role = "user", content = "hi" } },
                        max_tokens = 1
                    };
                    request.Content = JsonContent.Create(body);

                    var response = await httpClient.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        reports.Add("Groq AI API is online and authenticated.");
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync(cancellationToken);
                        reports.Add($"Groq AI API returned failure status ({(int)response.StatusCode}): {err}");
                        hasIssues = true;
                    }
                }
                catch (Exception ex)
                {
                    reports.Add($"Groq AI API check failed: {ex.Message}");
                    hasIssues = true;
                }
            }

            string combinedReport = string.Join(" | ", reports);
            if (hasIssues)
            {
                return HealthCheckResult.Degraded($"External Services issue: {combinedReport}");
            }

            return HealthCheckResult.Healthy($"All external integrations are online: {combinedReport}");
        }
    }
}
