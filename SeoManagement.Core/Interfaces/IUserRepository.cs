using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IUserRepository
	{
		Task<(List<ApplicationUser> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<ApplicationUser> GetByIdAsync(int userId);
		Task AddAsync(ApplicationUser user);
		Task UpdateAsync(ApplicationUser user);
		Task DeleteAsync(int userId);
	}
}
