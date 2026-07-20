using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shtbly.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");

                // Check if it's an API/AJAX request
                bool isAjaxOrApi = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                                   context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                                   context.Request.Headers["Accept"].ToString().Contains("application/json");

                if (isAjaxOrApi)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    var response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = "An internal server error occurred.",
                        Detailed = ex.Message // You can hide this in production if desired
                    };

                    var jsonResponse = JsonSerializer.Serialize(response);
                    await context.Response.WriteAsync(jsonResponse);
                }
                else
                {
                    // Re-throw to let ASP.NET Core UseExceptionHandler handle the MVC redirect to /Customer/Home/Error
                    throw;
                }
            }
        }
    }
}
