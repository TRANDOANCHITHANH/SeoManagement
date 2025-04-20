using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class SiteRepository : ISiteRepository
	{
		private readonly AppDbContext _appDbContext;

		public SiteRepository(AppDbContext appDbContext)
		{
			_appDbContext = appDbContext;
		}

		public async Task<List<Site>> GetSitesByUserIdAsync(int userId)
		{
			return await _appDbContext.Sites.Where(s => s.UserId == userId).ToListAsync();
		}

		public async Task<Site> GetSiteByIdAsync(int siteId, int userId)
		{
			return await _appDbContext.Sites.FirstOrDefaultAsync(s => s.Id == siteId && s.UserId == userId);
		}

		public async Task AddSiteAsync(Site site)
		{
			_appDbContext.Sites.Add(site);
			await _appDbContext.SaveChangesAsync();
		}
	}
}
