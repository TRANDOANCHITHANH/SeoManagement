using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class BacklinkResultRepository : IBacklinkResultRepository
	{
		private readonly AppDbContext _context;

		public BacklinkResultRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(Backlink backlinkResult)
		{
			await _context.Backlinks.AddAsync(backlinkResult);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Backlink backlinkResult)
		{
			var existingResult = await _context.Backlinks.FindAsync(backlinkResult.BacklinkID);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(backlinkResult);
				existingResult.LastCheckedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<Backlink>> GetByProjectIdAsync(int projectId)
		{
			return await _context.Backlinks
				.Where(b => b.ProjectID == projectId)
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.Backlinks.FindAsync(id);
			if (result != null)
			{
				_context.Backlinks.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<Backlink> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}
	}
}
