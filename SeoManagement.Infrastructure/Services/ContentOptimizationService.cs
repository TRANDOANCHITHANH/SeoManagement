using Microsoft.Extensions.Logging;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SeoManagement.Infrastructure.Services
{
	public class ContentOptimizationService : IContentOptimizationService
	{
		private readonly IContentOptimizationRepository _contentOptimizationRepository;
		private readonly ILogger<ContentOptimizationService> _logger;
		public ContentOptimizationService(IContentOptimizationRepository contentOptimizationRepository, ILogger<ContentOptimizationService> logger)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
			_logger = logger;
		}
		public async Task<ContentOptimizationAnalysis> AnalyzeContentAsync(ContentOptimizationAnalysis request)
		{
			_logger.LogInformation("Starting AnalyzeContentAsync for ProjectId: {ProjectId}", request.ProjectID);
			var result = new ContentOptimizationAnalysis
			{
				ProjectID = request.ProjectID,
				TargetKeyword = request.TargetKeyword,
				Content = request.Content,
				AnalyzedAt = DateTime.Now,
			};

			// Keyword Usage
			int keywordCount = Regex.Matches(request.Content.ToLower(), Regex.Escape(request.TargetKeyword.ToLower())).Count;
			result.KeywordUsage = keywordCount == 0
				? "Use these keywords at least once."
				: $"One target keyword used {keywordCount} times.";

			// Keyword Density
			int totalWords = request.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
			double keywordDensity = (double)keywordCount / totalWords * 100;
			result.KeywordDensity = keywordDensity > 3
				? $"{keywordDensity:F1}% (Too high, reduce keyword usage to avoid stuffing)."
				: $"{keywordDensity:F1}% (Optimal, avoid exceeding 3%).";

			// Related Keywords (giả lập)
			result.RelatedKeywords = "SEO tips, content marketing, keyword research";

			// Alt Attribute Issues
			if (request.Content.Contains("<img") && !request.Content.Contains("alt="))
			{
				result.AltAttributeIssues = "Alt attribute issues: Some images lack alt attributes.";
			}
			else
			{
				result.AltAttributeIssues = "Alt attributes are present (if images exist).";
			}

			// Image Suggestion
			if (!request.Content.Contains("<img"))
			{
				result.ImageSuggestion = "Enrich your text with images to make it more appealing for the readers.";
			}
			else
			{
				result.ImageSuggestion = "Images are included in the content.";
			}

			// Title Issues
			if (!request.Content.Contains("<h1>") || !request.Content.ToLower().Contains(request.TargetKeyword.ToLower()))
			{
				result.TitleIssues = "Title issues: Consider including the target keyword in an H1 tag.";
			}
			else
			{
				result.TitleIssues = "Title is optimized with the target keyword.";
			}

			// Meta Suggestions (giả lập)
			result.MetaSuggestions = "Include target keyword in meta title, keep under 60 characters.";

			// Word Count
			result.WordCount = $"Words: {totalWords}, Target: 501. Consider adding more content to reach the target.";

			// Readability Score (giả lập)
			result.ReadabilityScore = "65 (Good, aim for 60-70 for general audience).";

			// Tone of Voice (giả lập)
			result.ToneOfVoice = "Casual, consider making it more formal for academic content.";

			// Originality Check (giả lập)
			result.OriginalityCheck = "98% original, 2% matches found.";

			// Content Structure Issues (giả lập)
			result.ContentStructureIssues = "Add more subheadings (H2, H3) for better readability.";

			// Link Issues (giả lập)
			result.LinkIssues = "Found 1 broken link, consider adding internal links to related posts.";

			// Lưu vào cơ sở dữ liệu
			await _contentOptimizationRepository.AddAsync(result);

			return result;
		}
		public async Task<ContentOptimizationAnalysis> GetByProjectIdAsync(int projectId)
		{
			var analyses = await _contentOptimizationRepository.GetByProjectIdAsync(projectId);
			var latestAnalysis = analyses.OrderByDescending(a => a.AnalyzedAt).FirstOrDefault();
			return latestAnalysis;
		}
	}
}
