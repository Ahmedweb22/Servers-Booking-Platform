using Hangfire.Dashboard;

namespace Shtbly.Utilities
{
    public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var user = context.GetHttpContext().User;

            return user.Identity?.IsAuthenticated == true &&
                   (user.IsInRole(SD.ROLE_ADMIN) || user.IsInRole(SD.ROLE_SUPER_ADMIN));
        }
    }
}
