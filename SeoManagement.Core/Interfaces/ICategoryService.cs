using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ICategoryService
	{
		Task<(List<Category> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<List<Category>> GetActiveCategoriesAsync();
		Task<Category> GetByIdAsync(int id);
		Task CreateAsync(Category category);
		Task UpdateAsync(Category category);
		Task DeleteAsync(int id);
	}
}
