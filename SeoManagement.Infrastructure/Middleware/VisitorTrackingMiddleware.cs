using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SeoManagement.Infrastructure.Services;

namespace SeoManagement.Infrastructure.Middleware
{
	public class VisitorTrackingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly VisitorCounterService _visitorService;

		public VisitorTrackingMiddleware(RequestDelegate next, VisitorCounterService visitorService)
		{
			_next = next;
			_visitorService = visitorService;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var path = context.Request.Path.ToString().ToLower();
			if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".jpg") || path.EndsWith(".png"))
			{
				await _next(context);
				return;
			}

			var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();
			if (userAgent.Contains("bot") || userAgent.Contains("crawler"))
			{
				await _next(context);
				return;
			}

			if (!path.StartsWith("/admin") && context.User?.Identity?.IsAuthenticated == true)
			{
				var userId = context.User.Identity.Name;
				if (!string.IsNullOrEmpty(userId))
				{
					await _visitorService.RegisterVisitAsync(userId);
				}
			}

			await _next(context);
		}
	}

	public static class VisitorTrackingMiddlewareExtensions
	{
		public static IApplicationBuilder UseVisitorTracking(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<VisitorTrackingMiddleware>();
		}
	}
}