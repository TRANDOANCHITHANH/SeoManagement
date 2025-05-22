using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOPerformanceRepository : IRepository<SEOPerformanceHistory>
	{
		Task<List<SEOPerformanceHistory>> GetHistoryByProjectIdAsync(int projectId);
	}
}
