using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class SEOProjectService : ISEOProjectService
	{
		private readonly ISEOProjectRepository _seoProjectRepository;

		public SEOProjectService(ISEOProjectRepository repository)
		{
			_seoProjectRepository = repository;
		}

		public async Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			return await _seoProjectRepository.GetPagedAsync(pageNumber, pageSize);
		}

		public async Task<SEOProject> GetByIdAsync(int projectId)
		{
			return await _seoProjectRepository.GetByIdAsync(projectId);
		}

		public async Task CreateSEOProjectAsync(SEOProject project)
		{
			await _seoProjectRepository.AddAsync(project);
		}

		public async Task UpdateSEOProjectAsync(SEOProject project)
		{
			await _seoProjectRepository.UpdateAsync(project);
		}

		public async Task DeleteSEOProjectAsync(int projectId)
		{
			await _seoProjectRepository.DeleteAsync(projectId);
		}
	}
}
