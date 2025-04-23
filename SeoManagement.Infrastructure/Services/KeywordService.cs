using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class KeywordService : IKeywordService
	{
		private readonly IKeywordRepository _keywordRepository;

		public KeywordService(IKeywordRepository keywordRepository)
		{
			_keywordRepository = keywordRepository;
		}

		public async Task<(IEnumerable<Keyword>, int)> GetPagedAsync(int projectId, int pageNumber, int pageSize)
		{
			return await _keywordRepository.GetPagedByProjectIdAsync(projectId, pageNumber, pageSize);
		}

		public async Task<Keyword> GetByIdAsync(int id)
		{
			return await _keywordRepository.GetByIdAsync(id);
		}

		public async Task CreateKeywordAsync(Keyword keyword)
		{
			await _keywordRepository.AddAsync(keyword);
		}

		public async Task UpdateKeywordAsync(Keyword keyword)
		{
			await _keywordRepository.UpdateAsync(keyword);
		}

		public async Task DeleteKeywordAsync(int id)
		{
			await _keywordRepository.DeleteAsync(id);
		}
	}
}
