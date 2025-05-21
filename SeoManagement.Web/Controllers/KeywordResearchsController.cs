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
	public class KeywordResearchsController : Controller
	{
		private readonly IUserService _userService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IKeywordResearchService _keywordResearchService;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<KeywordResearchsController> _logger;

		public KeywordResearchsController(
			IUserService userService,
			UserManager<ApplicationUser> userManager,

			IKeywordResearchService keywordResearchService,
			ISEOProjectService projectService,
			ILogger<KeywordResearchsController> logger)
		{
			_userService = userService;
			_userManager = userManager;
			_keywordResearchService = keywordResearchService;
			_projectService = projectService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> IndexResearch(int? projectId = null)
		{
			var model = new KeywordResearchViewModel { ProjectId = projectId ?? 0 };
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var seedKeywords = await _keywordResearchService.GetByProjectIdAsync(projectId.Value);

				// Kiểm tra null và khởi tạo danh sách rỗng nếu cần
				if (seedKeywords == null)
				{
					seedKeywords = new List<SeedKeyword>();
				}

				var mainKeywords = seedKeywords.Select(sk => new KeywordResearchViewModel.KeywordViewModel
				{
					SeedKeyword = sk.Keyword,
					SuggestedKeyword = sk.Keyword,
					SearchVolume = sk.SearchVolume,
					Difficulty = sk.Difficulty,
					CPC = sk.CPC,
					CreatedDate = sk.CreatedDate.ToString("yyyy-MM-dd"),
					CompetitionValue = sk.CompetitionValue,
					MonthlySearchVolumes = sk.MonthlySearchVolumes.Select(m => new KeywordResearchViewModel.MonthlyVolumeViewModel
					{
						Month = m.Month,
						Year = m.Year,
						Searches = m.Searches
					}).ToList(),
					IsMainKeyword = true
				}).DistinctBy(k => k.SeedKeyword);

				var relatedKeywords = seedKeywords
					.SelectMany(sk => sk.RelatedKeywords)
					.Select(rk => new KeywordResearchViewModel.KeywordViewModel
					{
						SeedKeyword = rk.SeedKeyword.Keyword,
						SuggestedKeyword = rk.SuggestedKeyword,
						SearchVolume = rk.SearchVolume,
						Difficulty = rk.Difficulty,
						CPC = rk.CPC,
						CreatedDate = rk.CreatedDate.ToString("yyyy-MM-dd"),
						CompetitionValue = rk.CompetitionValue,
						MonthlySearchVolumes = rk.MonthlySearchVolumes.Select(m => new KeywordResearchViewModel.MonthlyVolumeViewModel
						{
							Month = m.Month,
							Year = m.Year,
							Searches = m.Searches
						}).ToList(),
						IsMainKeyword = false
					});

				model.Keywords = mainKeywords.Concat(relatedKeywords).ToList();

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
			}

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> IndexResearch(KeywordResearchViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.SeedKeyword) || model.ProjectId <= 0)
			{
				TempData["Error"] = "Vui lòng nhập từ khóa hợp lệ.";
				return RedirectToAction("IndexResearch", model);
			}


			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login", "Account");
			}

			if (!await _userService.CanPerformActionAsync(user.Id, ActionType.KeywordResearch.ToString()))
			{
				TempData["Error"] = "Bạn đã vượt quá giới hạn nghiên cứu từ khóa mỗi ngày.";
				return RedirectToAction("IndexResearch", model);
			}

			try
			{
				var seedKeywords = await _keywordResearchService.ResearchKeywordsAsync(model.ProjectId, model.SeedKeyword);

				var mainKeywords = seedKeywords.Select(sk => new KeywordResearchViewModel.KeywordViewModel
				{
					SeedKeyword = sk.Keyword,
					SuggestedKeyword = sk.Keyword,
					SearchVolume = sk.SearchVolume,
					Difficulty = sk.Difficulty,
					CPC = sk.CPC,
					CreatedDate = sk.CreatedDate.ToString("yyyy-MM-dd"),
					CompetitionValue = sk.CompetitionValue,
					MonthlySearchVolumes = sk.MonthlySearchVolumes.Select(m => new KeywordResearchViewModel.MonthlyVolumeViewModel
					{
						Month = m.Month,
						Year = m.Year,
						Searches = m.Searches
					}).ToList(),
					IsMainKeyword = true
				}).DistinctBy(k => k.SeedKeyword);

				var relatedKeywords = seedKeywords
					.SelectMany(sk => sk.RelatedKeywords)
					.Select(rk => new KeywordResearchViewModel.KeywordViewModel
					{
						SeedKeyword = rk.SeedKeyword.Keyword,
						SuggestedKeyword = rk.SuggestedKeyword,
						SearchVolume = rk.SearchVolume,
						Difficulty = rk.Difficulty,
						CPC = rk.CPC,
						CreatedDate = rk.CreatedDate.ToString("yyyy-MM-dd"),
						CompetitionValue = rk.CompetitionValue,
						MonthlySearchVolumes = rk.MonthlySearchVolumes.Select(m => new KeywordResearchViewModel.MonthlyVolumeViewModel
						{
							Month = m.Month,
							Year = m.Year,
							Searches = m.Searches
						}).ToList(),
						IsMainKeyword = false
					});

				model.Keywords = mainKeywords.Concat(relatedKeywords).ToList();

				var project = await _projectService.GetByIdAsync(model.ProjectId);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
				ViewBag.ProjectId = model.ProjectId;
				model.Message = "Phân tích từ khóa thành công!";
				await _userService.IncrementActionCountAsync(user.Id, ActionType.KeywordResearch.ToString());
				TempData["Success"] = "Phân tích thành công!";
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi phân tích từ khóa: {SeedKeyword}", model.SeedKeyword);
				model.Message = $"Lỗi: {ex.Message}";
				TempData["Error"] = $"Lỗi khi phân tích từ khóa: {ex.Message}";
			}

			return RedirectToAction("IndexResearch", new { model.ProjectId });
		}

		[HttpPost]
		public async Task<IActionResult> DeleteKeyword(int projectId, string seedKeyword)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var suggestion = (await _keywordResearchService.GetByProjectIdAsync(projectId))
					.FirstOrDefault(w => w.Keyword == seedKeyword);
				if (suggestion == null)
				{
					TempData["Error"] = "Từ khóa không tồn tại trong dự án.";
					return RedirectToAction(nameof(IndexResearch), new { projectId });
				}

				await _keywordResearchService.DeleteAsync(suggestion.Id);

				TempData["Success"] = "Xóa Từ khóa thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa từ khóa: {seedKeyword} trong dự án: {ProjectId}", seedKeyword, projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xóa domain: " + ex.Message;
			}

			return RedirectToAction(nameof(IndexResearch), new { projectId });
		}
	}
}
