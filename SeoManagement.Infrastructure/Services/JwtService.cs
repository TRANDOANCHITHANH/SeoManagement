using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SeoManagement.Infrastructure.Services
{

	public class JwtService : IJwtService
	{
		private readonly IConfiguration _configuration;
		private readonly UserManager<ApplicationUser> _userManager;

		public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
		{
			_configuration = configuration;
			_userManager = userManager;
		}

		public async Task<string> GenerateToken(ClaimsPrincipal user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.Identity.Name),
				new Claim(ClaimTypes.Role, (await _userManager.GetRolesAsync(await _userManager.GetUserAsync(user))).FirstOrDefault() ?? "")
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _configuration["Jwt:Issuer"],
				audience: _configuration["Jwt:Audience"],
				claims: claims,
				expires: DateTime.Now.AddHours(1),
				signingCredentials: creds);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
