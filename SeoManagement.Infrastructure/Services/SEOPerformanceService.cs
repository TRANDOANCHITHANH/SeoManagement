using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class SEOPerformanceService : ISEOPerformanceService
	{
		private readonly ISEOPerformanceRepository _repository;
		private readonly IService<Keyword> _keywordService;
		private readonly IPageSpeedResultService _pageSpeedResultService;
		private readonly IBacklinkResultService _backlinkResultService;
		private readonly IIndexCheckerUrlService _indexCheckerUrlService;
		private readonly ISEOOnPageCheckService _seoOnPageCheckService;
		private readonly ISEOProjectService _seoProjectService;

		public SEOPerformanceService(ISEOPerformanceRepository repository, IService<Keyword> keywordService, IPageSpeedResultService pageSpeedResultService, IBacklinkResultService backlinkResultService, IIndexCheckerUrlService indexCheckerUrlService, ISEOOnPageCheckService seoOnPageCheckService, ISEOProjectService seoProjectService)
		{
			_repository = repository;
			_keywordService = keywordService;
			_pageSpeedResultService = pageSpeedResultService;
			_backlinkResultService = backlinkResultService;
			_indexCheckerUrlService = indexCheckerUrlService;
			_seoOnPageCheckService = seoOnPageCheckService;
			_seoProjectService = seoProjectService;
		}

		public async Task RecordPerformanceAsync(int projectId)
		{
			var project = await _seoProjectService.GetByIdAsync(projectId);
			if (project == null) throw new Exception("Dự án không tồn tại.");

			var history = new SEOPerformanceHistory
			{
				ProjectId = projectId,
				ProjectType = project.ProjectType,
				RecordedAt = DateTime.UtcNow
			};

			switch (project.ProjectType)
			{
				case "KeywordRankChecker":
					await RecordKeywordPerformance(history, projectId);
					break;
				case "SEOOnPage":
					await RecordOnPagePerformance(history, projectId);
					break;
				case "PageSpeedChecker":
					await RecordPageSpeedPerformance(history, projectId);
					break;
				case "BacklinkChecker":
					await RecordBacklinkPerformance(history, projectId);
					break;
				case "IndexChecker":
					await RecordIndexPerformance(history, projectId);
					break;
				default:
					break;
			}

			await _repository.AddAsync(history);
		}

		private async Task RecordKeywordPerformance(SEOPerformanceHistory history, int projectId)
		{
			var keywords = await _keywordService.GetByProjectIdAsync(projectId);
			history.AverageKeywordRank = keywords
				.Where(k => k.TopPosition.HasValue)
				.Average(k => (double)k.TopPosition.Value);
		}

		private async Task RecordOnPagePerformance(SEOPerformanceHistory history, int projectId)
		{
			var (onPageChecks, _) = await _seoOnPageCheckService.GetPagedAsync(projectId, 1, 10);
			history.AverageOnPageScore = onPageChecks.Average(c => CalculateOnPageScore(c));

			// Sử dụng dữ liệu PageSpeedResult hiện có (nếu có)
			var pageSpeedResults = await _pageSpeedResultService.GetByProjectIdAsync(projectId);
			if (pageSpeedResults.Any())
			{
				var latestResult = pageSpeedResults.OrderByDescending(r => r.LastCheckedDate).First();
				history.PageSpeedScore = latestResult.LCP ?? latestResult.LoadTime;
			}
		}

		private async Task RecordPageSpeedPerformance(SEOPerformanceHistory history, int projectId)
		{
			var pageSpeedResults = await _pageSpeedResultService.GetByProjectIdAsync(projectId);
			if (pageSpeedResults.Any())
			{
				var latestResult = pageSpeedResults.OrderByDescending(r => r.LastCheckedDate).First();
				history.PageSpeedScore = latestResult.LCP ?? latestResult.LoadTime;
			}
		}

		private async Task RecordBacklinkPerformance(SEOPerformanceHistory history, int projectId)
		{
			var backlinks = await _backlinkResultService.GetByProjectIdAsync(projectId);
			if (backlinks.Any())
			{
				var latestBacklink = backlinks.OrderByDescending(b => b.LastCheckedDate).First();
				history.BacklinkCount = latestBacklink.TotalBacklinks;
			}
		}

		private async Task RecordIndexPerformance(SEOPerformanceHistory history, int projectId)
		{
			var indexUrls = await _indexCheckerUrlService.GetByProjectIdAsync(projectId);
			history.IndexedPageCount = indexUrls.Count(u => u.IsIndexed == true);
			history.UnindexedPageCount = indexUrls.Count(u => u.IsIndexed == false);
		}

		public async Task<List<SEOPerformanceHistory>> GetHistoryByProjectIdAsync(int projectId)
		{
			return await _repository.GetHistoryByProjectIdAsync(projectId);
		}

		private double CalculateOnPageScore(SEOOnPageCheck check)
		{
			double score = 0;
			double maxScore = 6;
			if (check.Title?.Length >= 30 && check.Title.Length <= 60) score += 1;
			if (check.MetaDescription?.Length >= 120 && check.MetaDescription.Length <= 160) score += 1;
			if (check.MainKeyword != null && check.Title != null && check.Title.ToLower().Contains(check.MainKeyword.ToLower())) score += 1;
			if (check.MainKeyword != null && check.MetaDescription != null && check.MetaDescription.ToLower().Contains(check.MainKeyword.ToLower())) score += 1;
			if (check.WordCount >= 300) score += 1;
			if (check.CreatedAt != default) score += 1;
			return (score / maxScore) * 100;
		}
	}
}
