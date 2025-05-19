using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IUserRepository : IRepository<ApplicationUser>
	{
		Task<(List<ApplicationUser> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<UserActionLimit> GetUserActionLimitAsync(int userId, string actionType);
		Task AddOrUpdateUserActionLimitAsync(UserActionLimit actionLimit);
		DbContext GetContext();
	}
}
