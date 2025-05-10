using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class NewService : INewsService
	{
		private readonly INewsRepository _repository;

		public NewService(INewsRepository repository)
		{
			_repository = repository;
		}

		public async Task<(List<New> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			return await _repository.GetPagedAsync(pageNumber, pageSize);
		}

		public async Task<New> GetByIdAsync(int newId)
		{
			return await _repository.GetByIdAsync(newId);
		}

		public async Task CreateAsync(New news)
		{
			if (string.IsNullOrWhiteSpace(news.Title) || string.IsNullOrWhiteSpace(news.Content))
				throw new ArgumentException("Title and Content are required.");

			await _repository.AddAsync(news);
		}

		public async Task UpdateAsync(New news)
		{
			var existingNews = await _repository.GetByIdAsync(news.NewsID);
			if (existingNews == null)
				throw new KeyNotFoundException("News not found.");

			await _repository.UpdateAsync(news);
		}

		public async Task DeleteAsync(int newId)
		{
			var news = await _repository.GetByIdAsync(newId);
			if (news == null)
				throw new KeyNotFoundException("News not found.");

			await _repository.DeleteAsync(newId);
		}
	}
}
