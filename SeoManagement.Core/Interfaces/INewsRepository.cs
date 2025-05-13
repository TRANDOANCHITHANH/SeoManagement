using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface INewsRepository : IRepository<New>
	{
		Task<(List<New> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, bool? isPublished = null);
		Task<int> GetTotalCountAsync();
	}
}
