using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class CategoryRepository : ICategoryRepository
	{
		private readonly AppDbContext _context;

		public CategoryRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<(List<Category> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{

			if (pageNumber < 1 || pageSize < 1)
				throw new ArgumentException("PageNumber and PageSize must be positive.");
			var query = _context.Categories.AsQueryable();
			query = query.OrderByDescending(n => n.CreatedDate);
			var totalItems = await GetTotalCountAsync();
			var items = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalItems);
		}

		public async Task<int> GetTotalCountAsync()
		{
			return await _context.News.CountAsync();
		}

		public async Task<Category> GetByIdAsync(int id)
		{
			return await _context.Categories.FindAsync(id);
		}

		public async Task AddAsync(Category entity)
		{
			await _context.Categories.AddAsync(entity);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Category entity)
		{
			_context.Entry(entity).State = EntityState.Modified;
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int categoryId)
		{
			var categories = await _context.Categories.FindAsync(categoryId);
			if (categoryId != null)
			{
				_context.Categories.Remove(categories);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<Category>> GetActiveCategoriesAsync()
		{
			return await _context.Categories
				.Where(c => c.IsActive)
				.OrderBy(c => c.Name)
				.ToListAsync();
		}

		public async Task<bool> IsSlugExistsAsync(string slug, int? categoryId = null)
		{
			var query = _context.Categories.AsQueryable();
			if (categoryId.HasValue)
			{
				query = query.Where(c => c.CategoryId != categoryId.Value);
			}
			return await query.AnyAsync(c => c.Slug == slug);
		}
	}
}
