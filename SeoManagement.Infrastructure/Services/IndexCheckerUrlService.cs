using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class IndexCheckerUrlService : IIndexCheckerUrlService
	{
		private readonly IIndexCheckerUrlRepository _repository;
		public IndexCheckerUrlService(IIndexCheckerUrlRepository indexCheckerUrlRepository)
		{
			_repository = indexCheckerUrlRepository;
		}
		public async Task AddAsync(IndexCheckerUrl url)
		{
			await _repository.AddAsync(url);
		}
		public async Task UpdateAsync(IndexCheckerUrl url)
		{
			await _repository.UpdateAsync(url);
		}
		public async Task<List<IndexCheckerUrl>> GetByProjectIdAsync(int projectId)
		{
			return await _repository.GetByProjectIdAsync(projectId);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
		}
	}
}
