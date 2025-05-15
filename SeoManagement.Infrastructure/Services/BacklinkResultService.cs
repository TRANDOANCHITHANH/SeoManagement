using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class BacklinkResultService : IBacklinkResultService
	{
		private readonly IBacklinkResultRepository _repository;

		public BacklinkResultService(IBacklinkResultRepository repository)
		{
			_repository = repository;
		}

		public async Task AddAsync(Backlink result)
		{
			await _repository.AddAsync(result);
		}

		public async Task UpdateAsync(Backlink result)
		{
			await _repository.UpdateAsync(result);
		}

		public async Task<List<Backlink>> GetByProjectIdAsync(int projectId)
		{
			return await _repository.GetByProjectIdAsync(projectId);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
		}
	}
}
