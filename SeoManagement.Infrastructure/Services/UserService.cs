using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class UserService : IUserService
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole<int>> _roleManager;

		public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager)
		{
			_userManager = userManager;
			_roleManager = roleManager;
		}

		public async Task<(IEnumerable<ApplicationUser>, int)> GetPagedAsync(int pageNumber, int pageSize)
		{
			var query = _userManager.Users;
			var totalItems = await query.CountAsync();
			var users = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			return (users, totalItems);
		}

		public async Task<ApplicationUser> GetByIdAsync(int id)
		{
			return await _userManager.FindByIdAsync(id.ToString());
		}

		public async Task CreateUserAsync(ApplicationUser user, string password, string role)
		{
			var result = await _userManager.CreateAsync(user, password);
			if (!result.Succeeded)
			{
				throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
			}

			if (!string.IsNullOrEmpty(role))
			{
				await _userManager.AddToRoleAsync(user, role);
			}
		}

		public async Task UpdateUserAsync(ApplicationUser user, string role)
		{
			var existingUser = await _userManager.FindByIdAsync(user.Id.ToString());
			if (existingUser == null)
			{
				throw new Exception("User not found");
			}

			existingUser.UserName = user.UserName;
			existingUser.Email = user.Email;
			existingUser.FullName = user.FullName;
			existingUser.IsActive = user.IsActive;

			var result = await _userManager.UpdateAsync(existingUser);
			if (!result.Succeeded)
			{
				throw new Exception($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
			}

			if (!string.IsNullOrEmpty(role))
			{
				var currentRoles = await _userManager.GetRolesAsync(existingUser);
				await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
				await _userManager.AddToRoleAsync(existingUser, role);
			}
		}

		public async Task DeleteUserAsync(int id)
		{
			var user = await _userManager.FindByIdAsync(id.ToString());
			if (user == null)
			{
				throw new Exception("User not found");
			}

			var result = await _userManager.DeleteAsync(user);
			if (!result.Succeeded)
			{
				throw new Exception($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
			}
		}

		public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
		{
			return await _userManager.GetRolesAsync(user);
		}
	}
}