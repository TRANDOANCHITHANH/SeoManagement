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

		public async Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, int? userId = null)
		{
			return await _seoProjectRepository.GetPagedAsync(pageNumber, pageSize, userId);
		}

		public async Task<SEOProject> GetByIdAsync(int projectId)
		{
			return await _seoProjectRepository.GetByIdAsync(projectId);
		}

		public async Task CreateSEOProjectAsync(SEOProject project)
		{
			if (project.StartDate > DateTime.Now)
				throw new ArgumentException("Start date cannot be in the future");

			await _seoProjectRepository.AddAsync(project);
		}

		public async Task UpdateSEOProjectAsync(SEOProject project)
		{
			var existing = await _seoProjectRepository.GetByIdAsync(project.ProjectID);
			if (existing == null)
				throw new KeyNotFoundException("Project not found");

			//if (existing.Status == ProjectStatus.Completed && project.Status != ProjectStatus.Completed)
			//	throw new InvalidOperationException("Cannot modify completed projects");

			await _seoProjectRepository.UpdateAsync(project);
		}

		public async Task DeleteSEOProjectAsync(int projectId)
		{
			await _seoProjectRepository.DeleteAsync(projectId);
		}

		public async Task<List<SEOProject>> GetAllAsync()
		{
			return await _seoProjectRepository.GetAllAsync();
		}
	}
}
