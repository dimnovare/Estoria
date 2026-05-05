using Hangfire.Dashboard;

namespace Estoria.Api.Configuration;

/// <summary>
/// Hangfire dashboard auth: lets only authenticated users in the Admin role view
/// the /hangfire UI. Anonymous callers get 401, non-Admin authenticated users get
/// 401 as well — Hangfire returns 401 from any falsy filter result and stops
/// rendering the dashboard.
/// </summary>
public class AdminOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole("Admin");
    }
}
