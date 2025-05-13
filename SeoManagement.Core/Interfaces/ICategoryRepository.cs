using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ICategoryRepository : IRepository<Category>
	{
		Task<List<Category>> GetActiveCategoriesAsync();
		Task<(List<Category> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<bool> IsSlugExistsAsync(string slug, int? excludeCategoryId = null);
	}
}
