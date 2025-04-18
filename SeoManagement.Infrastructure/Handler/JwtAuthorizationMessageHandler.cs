using Microsoft.AspNetCore.Http;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Handler
{
	public class JwtAuthorizationMessageHandler : DelegatingHandler
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IJwtService _jwtService;

		public JwtAuthorizationMessageHandler(IHttpContextAccessor httpContextAccessor, IJwtService jwtService)
		{
			_httpContextAccessor = httpContextAccessor;
			_jwtService = jwtService;
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var user = _httpContextAccessor.HttpContext?.User;
			if (user?.Identity?.IsAuthenticated == true)
			{
				var token = await _jwtService.GenerateToken(user);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
			}
			return await base.SendAsync(request, cancellationToken);
		}
	}
}
