using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IContentOptimizationService
	{
		Task<ContentOptimizationAnalysis> AnalyzeContentAsync(ContentOptimizationAnalysis request);
		Task<ContentOptimizationAnalysis> GetByProjectIdAsync(int projectId);
	}
}
