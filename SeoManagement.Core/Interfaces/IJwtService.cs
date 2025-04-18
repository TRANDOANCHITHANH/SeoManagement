using System.Security.Claims;

namespace SeoManagement.Core.Interfaces
{
	public interface IJwtService
	{
		Task<string> GenerateToken(ClaimsPrincipal user);
	}
}
