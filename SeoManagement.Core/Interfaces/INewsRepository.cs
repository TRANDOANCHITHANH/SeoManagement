using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface INewsRepository
	{
		Task<(List<New> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<New> GetByIdAsync(int newId);
		Task AddAsync(New news);
		Task UpdateAsync(New news);
		Task DeleteAsync(int newId);
		Task<int> GetTotalCountAsync();
	}
}
