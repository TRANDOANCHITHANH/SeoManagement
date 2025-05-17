using Hangfire.Dashboard;

namespace SeoManagement.Web.Utilities
{
	public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
	{
		private readonly ILogger<HangfireDashboardAuthorizationFilter> _logger;

		public HangfireDashboardAuthorizationFilter(ILogger<HangfireDashboardAuthorizationFilter> logger = null)
		{
			_logger = logger;
		}

		public bool Authorize(DashboardContext context)
		{
			var httpContext = context.GetHttpContext();
			var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
			var isAdmin = httpContext.User.IsInRole("Admin");

			if (_logger != null)
			{
				_logger.LogInformation("Hangfire Dashboard Access Attempt: IsAuthenticated={IsAuthenticated}, IsAdmin={IsAdmin}, User={UserName}",
					isAuthenticated, isAdmin, httpContext.User.Identity?.Name ?? "Anonymous");
			}

			return isAuthenticated && isAdmin;
		}
	}
}
