using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IContentOptimizationRepository : IRepository<ContentOptimizationAnalysis>
	{
		Task<List<ContentOptimizationAnalysis>> GetByProjectIdAsync(int projectId);
	}
}
