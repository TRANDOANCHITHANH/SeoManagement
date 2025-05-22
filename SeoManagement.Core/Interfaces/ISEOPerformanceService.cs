using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOPerformanceService
	{
		Task RecordPerformanceAsync(int projectId);
		Task<List<SEOPerformanceHistory>> GetHistoryByProjectIdAsync(int projectId);
	}
}
