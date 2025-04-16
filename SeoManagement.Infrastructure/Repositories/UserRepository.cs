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

			var query = _context.Users.AsQueryable();

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
			return await _context.Users.FindAsync(userId);
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
	}
}