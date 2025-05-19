using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly AppDbContext _context;

		public UserRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<(List<ApplicationUser> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			if (pageNumber < 1 || pageSize < 1)
				throw new ArgumentException("Page number and page size must be greater than 0.");

			var query = _context.Users.AsQueryable().Include(u => u.ActionLimits);

			var totalItems = await query.CountAsync();
			var items = await query
				.OrderBy(u => u.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalItems);
		}

		public async Task<ApplicationUser> GetByIdAsync(int userId)
		{
			return await _context.Users
			   .Include(u => u.ActionLimits)
			   .FirstOrDefaultAsync(u => u.Id == userId);
		}

		public async Task AddAsync(ApplicationUser user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			await _context.Users.AddAsync(user);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(ApplicationUser user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			_context.Users.Update(user);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int userId)
		{
			var user = await GetByIdAsync(userId);
			if (user == null)
				throw new Exception("User not found.");

			_context.Users.Remove(user);
			await _context.SaveChangesAsync();
		}

		public async Task<UserActionLimit> GetUserActionLimitAsync(int userId, string actionType)
		{
			return await _context.UserActionLimits.FirstOrDefaultAsync(u => u.UserId == userId && u.ActionType == actionType);
		}

		public async Task AddOrUpdateUserActionLimitAsync(UserActionLimit actionLimit)
		{
			var existingLimit = await GetUserActionLimitAsync(actionLimit.UserId, actionLimit.ActionType);
			if (existingLimit != null)
			{
				existingLimit.DailyLimit = actionLimit.DailyLimit;
				existingLimit.ActionsToday = actionLimit.ActionsToday;
				existingLimit.LastActionDate = actionLimit.LastActionDate;
				_context.UserActionLimits.Update(existingLimit);
			}
			else
			{
				await _context.UserActionLimits.AddAsync(actionLimit);
			}
			await _context.SaveChangesAsync();
		}

		public DbContext GetContext()
		{
			return _context;
		}
	}
}