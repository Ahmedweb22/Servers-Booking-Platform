using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Shatbly.Controllers
{
    [Route("[controller]/[action]")]
    public class CultureController : Controller
    {
        [HttpGet]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
