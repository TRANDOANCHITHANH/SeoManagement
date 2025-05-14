using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class PageSpeedResultService : IPageSpeedResultService
	{
		private readonly IPageSpeedResultRepository _repository;

		public PageSpeedResultService(IPageSpeedResultRepository repository)
		{
			_repository = repository;
		}

		public async Task AddAsync(PageSpeedResult result)
		{
			await _repository.AddAsync(result);
		}

		public async Task UpdateAsync(PageSpeedResult result)
		{
			await _repository.UpdateAsync(result);
		}

		public async Task<List<PageSpeedResult>> GetByProjectIdAsync(int projectId)
		{
			return await _repository.GetByProjectIdAsync(projectId);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
		}
	}
}
