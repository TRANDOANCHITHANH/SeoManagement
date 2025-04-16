using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IUserService
	{
		Task<(IEnumerable<ApplicationUser>, int)> GetPagedAsync(int pageNumber, int pageSize);
		Task<ApplicationUser> GetByIdAsync(int id);
		Task CreateUserAsync(ApplicationUser user, string password, string role);
		Task UpdateUserAsync(ApplicationUser user, string role);
		Task DeleteUserAsync(int id);
		Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
	}
}
