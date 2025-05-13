using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly ICategoryRepository _repository;

		public CategoryService(ICategoryRepository repository)
		{
			_repository = repository;
		}

		public async Task<(List<Category> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			return await _repository.GetPagedAsync(pageNumber, pageSize);
		}

		public async Task<Category> GetByIdAsync(int id)
		{
			var category = await _repository.GetByIdAsync(id);
			if (category == null)
			{
				throw new KeyNotFoundException("Category not found.");
			}
			return category;
		}

		public async Task CreateAsync(Category category)
		{
			if (string.IsNullOrWhiteSpace(category.Name))
				throw new ArgumentException("Category name is required.");

			if (await _repository.IsSlugExistsAsync(category.Slug))
				throw new ArgumentException("Slug đã tồn tại. Vui lòng chọn Slug khác.");

			category.CreatedDate = DateTime.Now;
			category.IsActive = true;

			await _repository.AddAsync(category);
		}

		public async Task UpdateAsync(Category category)
		{
			if (string.IsNullOrWhiteSpace(category.Name))
				throw new ArgumentException("Category name is required.");

			var existingCategory = await GetByIdAsync(category.CategoryId);
			if (existingCategory == null)
				throw new KeyNotFoundException("Categories not found.");

			if (await _repository.IsSlugExistsAsync(category.Slug, category.CategoryId))
				throw new ArgumentException("Slug đã tồn tại. Vui lòng chọn Slug khác.");

			await _repository.UpdateAsync(existingCategory);
		}

		public async Task DeleteAsync(int id)
		{
			var category = await _repository.GetByIdAsync(id);
			if (category == null)
				throw new KeyNotFoundException("News not found.");

			await _repository.DeleteAsync(id);
		}

		public async Task<List<Category>> GetActiveCategoriesAsync()
		{
			return await _repository.GetActiveCategoriesAsync();
		}
	}
}
