using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Shtbly.Controllers
{
    [Route("[controller]/[action]")]
    public class CultureController : Controller
    {
        private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "ar"
        };

        [HttpGet]
        public IActionResult SetCulture(string? culture, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture) || !SupportedCultures.Contains(culture))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = true,
                    Secure = Request.IsHttps
                });

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }
    }
}
