using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Enum;
using SeoManagement.Core.Interfaces;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class ContentOptimizationController : Controller
	{
		private readonly IContentOptimizationService _optimizationService;
		private readonly ISEOProjectService _projectService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserService _userService;
		private readonly ILogger<ContentOptimizationController> _logger;

		public ContentOptimizationController(IContentOptimizationService optimizationService, ISEOProjectService projectService, UserManager<ApplicationUser> userManager, IUserService userService, ILogger<ContentOptimizationController> logger)
		{
			_optimizationService = optimizationService;
			_projectService = projectService;
			_userManager = userManager;
			_userService = userService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> Index(int? projectId = null)
		{
			var model = new ContentViewmodel { ProjectId = projectId ?? 0 };

			if (projectId.HasValue && projectId > 0)
			{
				var project = await _projectService.GetByIdAsync(projectId.Value);
				if (project == null)
				{
					TempData["Error"] = "Dự án không tồn tại.";
					return RedirectToAction("Index", "SEOProjects", new { projectType = "ContentOptimization" });
				}
				ViewBag.ProjectId = projectId;
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;

				var latestAnalysis = await _optimizationService.GetByProjectIdAsync(projectId.Value);
				if (latestAnalysis != null)
				{
					model.Result = new ContentViewmodel.ContentResultViewModel
					{
						KeywordUsage = latestAnalysis.KeywordUsage,
						KeywordDensity = latestAnalysis.KeywordDensity,
						RelatedKeywords = latestAnalysis.RelatedKeywords,
						AltAttributeIssues = latestAnalysis.AltAttributeIssues,
						ImageSuggestion = latestAnalysis.ImageSuggestion,
						TitleIssues = latestAnalysis.TitleIssues,
						MetaSuggestions = latestAnalysis.MetaSuggestions,
						WordCount = latestAnalysis.WordCount,
						ReadabilityScore = latestAnalysis.ReadabilityScore,
						ToneOfVoice = latestAnalysis.ToneOfVoice,
						OriginalityCheck = latestAnalysis.OriginalityCheck,
						ContentStructureIssues = latestAnalysis.ContentStructureIssues,
						LinkIssues = latestAnalysis.LinkIssues
					};
					model.TargetKeyword = latestAnalysis.TargetKeyword;
					model.Content = latestAnalysis.Content;
					model.Message = "Hiển thị kết quả phân tích gần nhất.";
				}
			}
			else
			{
				TempData["Error"] = "Vui lòng chọn một dự án.";
				return RedirectToAction("Index", "SEOProjects", new { projectType = "ContentOptimization" });
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Analyze(ContentViewmodel model)
		{
			_logger.LogInformation("Received Analyze request - ProjectId: {ProjectId}, TargetKeyword: {TargetKeyword}, Content length: {ContentLength}",
		model.ProjectId, model.TargetKeyword, model.Content?.Length ?? 0);

			if (string.IsNullOrWhiteSpace(model.TargetKeyword))
			{
				ModelState.AddModelError("TargetKeyword", "Từ khóa mục tiêu là bắt buộc.");
			}
			if (string.IsNullOrWhiteSpace(model.Content))
			{
				ModelState.AddModelError("Content", "Nội dung là bắt buộc.");
			}
			if (!ModelState.IsValid)
			{
				_logger.LogWarning("ModelState invalid. Errors: {Errors}",
					string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
				var project = await _projectService.GetByIdAsync(model.ProjectId);
				ViewBag.ProjectId = model.ProjectId;
				ViewBag.ProjectName = project?.ProjectName;
				ViewBag.ProjectDescription = project?.Description;
				return View("Index", model);
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login", "Account");
			}

			if (!await _userService.CanPerformActionAsync(user.Id, ActionType.ContentOptimization.ToString()))
			{
				TempData["Error"] = "Bạn đã vượt quá giới hạn phân tích nội dung mỗi ngày.";
				return RedirectToAction("Index", new { projectId = model.ProjectId });
			}

			try
			{
				var request = new ContentOptimizationAnalysis
				{
					ProjectID = model.ProjectId,
					TargetKeyword = model.TargetKeyword,
					Content = model.Content
				};

				_logger.LogInformation("Calling AnalyzeContentAsync with ProjectId: {ProjectId}, TargetKeyword: {TargetKeyword}",
			request.ProjectID, request.TargetKeyword);

				var result = await _optimizationService.AnalyzeContentAsync(request);
				_logger.LogInformation("AnalyzeContentAsync completed successfully for ProjectId: {ProjectId}", request.ProjectID);
				model.Result = new ContentViewmodel.ContentResultViewModel
				{
					KeywordUsage = result.KeywordUsage,
					KeywordDensity = result.KeywordDensity,
					RelatedKeywords = result.RelatedKeywords,
					AltAttributeIssues = result.AltAttributeIssues,
					ImageSuggestion = result.ImageSuggestion,
					TitleIssues = result.TitleIssues,
					MetaSuggestions = result.MetaSuggestions,
					WordCount = result.WordCount,
					ReadabilityScore = result.ReadabilityScore,
					ToneOfVoice = result.ToneOfVoice,
					OriginalityCheck = result.OriginalityCheck,
					ContentStructureIssues = result.ContentStructureIssues,
					LinkIssues = result.LinkIssues
				};
				var project = await _projectService.GetByIdAsync(model.ProjectId);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
				ViewBag.ProjectId = model.ProjectId;

				await _userService.IncrementActionCountAsync(user.Id, ActionType.ContentOptimization.ToString());
				TempData["Success"] = "Phân tích nội dung thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi phân tích nội dung: {TargetKeyword}", model.TargetKeyword);
				model.Message = $"Lỗi: {ex.Message}";
				TempData["Error"] = $"Lỗi khi phân tích nội dung: {ex.Message}";
				return RedirectToAction("Index", new { projectId = model.ProjectId });
			}

			return View("Index", model);
		}
		public async Task<IActionResult> History(int? projectId = null)
		{
			if (!projectId.HasValue || projectId <= 0)
			{
				TempData["Error"] = "Vui lòng chọn một dự án để xem lịch sử.";
				return RedirectToAction("Index");
			}
			var project = await _projectService.GetByIdAsync(projectId.Value);
			if (project == null)
			{
				TempData["Error"] = "Dự án không tồn tại.";
				return RedirectToAction("Index");
			}

			var analyses = await _optimizationService.GetByProjectIdAsync(projectId.Value);
			ViewBag.ProjectId = projectId;
			ViewBag.ProjectName = project.ProjectName;
			ViewBag.ProjectDescription = project.Description;

			return View(analyses);
		}
	}
}
