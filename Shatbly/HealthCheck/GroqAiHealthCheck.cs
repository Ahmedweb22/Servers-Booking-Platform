using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shatbly.HealthCheck
{
    public class GroqAiHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public GroqAiHealthCheck(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var apiKey = _config["GroqApi:ApiKey"];
            var model = _config["GroqApi:Model"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Unhealthy("Groq API Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                return HealthCheckResult.Unhealthy("Groq API Model is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);

                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Groq AI API is reachable and API Key is valid.");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    return HealthCheckResult.Unhealthy($"Groq AI API returned error status: {response.StatusCode}. Detail: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Groq AI connectivity check failed: {ex.Message}");
            }
        }
    }
}
