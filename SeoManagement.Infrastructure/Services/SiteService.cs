using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class SiteService : ISiteService
	{
		private readonly ISiteRepository _siteRepository;

		public SiteService(ISiteRepository siteRepository)
		{
			_siteRepository = siteRepository;
		}

		public async Task<List<Site>> GetSitesByUserIdAsync(int userId)
		{
			var sites = await _siteRepository.GetSitesByUserIdAsync(userId);
			return sites.Select(s => new Site
			{
				Id = s.Id,
				Name = s.Name,
				Url = s.Url,
				CreatedAt = s.CreatedAt
			}).ToList();
		}
		public async Task<Site> GetSiteByIdAsync(int siteId, int userId)
		{
			var site = await _siteRepository.GetSiteByIdAsync(siteId, userId);
			if (site == null) return null;
			return new Site
			{
				Id = site.Id,
				Name = site.Name,
				Url = site.Url,
				CreatedAt = site.CreatedAt
			};
		}
		public async Task AddSiteAsync(Site siteDto, int userId)
		{
			var site = new Site
			{
				Name = siteDto.Name,
				Url = siteDto.Url,
				UserId = userId,
				CreatedAt = DateTime.UtcNow
			};
			await _siteRepository.AddSiteAsync(site);
		}
	}
}
