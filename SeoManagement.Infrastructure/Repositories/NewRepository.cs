using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class NewRepository : INewsRepository
	{
		private readonly AppDbContext _context;

		public NewRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(New news)
		{
			await _context.News.AddAsync(news);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int newId)
		{
			var news = await _context.News.FindAsync(newId);
			if (news != null)
			{
				_context.News.Remove(news);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<New> GetByIdAsync(int newId)
		{
			return await _context.News.FindAsync(newId);
		}

		public async Task<(List<New> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, bool? isPublished = null)
		{

			if (pageNumber < 1 || pageSize < 1)
				throw new ArgumentException("PageNumber and PageSize must be positive.");
			var query = _context.News.AsQueryable();
			if (isPublished.HasValue)
			{
				query = query.Where(n => n.IsPublished == isPublished.Value);
			}
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

		public async Task UpdateAsync(New news)
		{
			var existingNews = await _context.News.FindAsync(news.NewsID);
			if (existingNews != null)
			{
				_context.Entry(existingNews).CurrentValues.SetValues(news);
				await _context.SaveChangesAsync();
			}
		}
	}
}
