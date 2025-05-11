using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface INewsService
	{
		Task<(List<New> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, bool? isPublished = null);
		Task<New> GetByIdAsync(int newId);
		Task CreateAsync(New news);
		Task UpdateAsync(New news);
		Task DeleteAsync(int newId);
	}
}
