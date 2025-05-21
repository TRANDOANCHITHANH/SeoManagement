using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Enum;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class KeywordResearchsController : Controller
	{
		private readonly IUserService _userService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly AppDbContext _context;
		private readonly IKeywordResearchService _keywordResearchService;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<KeywordResearchsController> _logger;

		public KeywordResearchsController(
			IUserService userService,
			UserManager<ApplicationUser> userManager,
			AppDbContext context,
			IKeywordResearchService keywordResearchService,
			ISEOProjectService projectService,
			ILogger<KeywordResearchsController> logger)
		{
			_userService = userService;
			_userManager = userManager;
			_context = context;
			_keywordResearchService = keywordResearchService;
			_projectService = projectService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> IndexResearch(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var previousResults = await _keywordResearchService.GetByProjectIdAsync(projectId.Value);

				// Kiểm tra null và khởi tạo danh sách rỗng nếu cần
				if (previousResults == null)
				{
					previousResults = new List<KeywordSuggestion>();
				}
				var mainKeywords = previousResults
					.Where(r => r.IsMainKeyword)
					.Select(r => new
					{
						SeedKeyword = r.SeedKeyword,
						SuggestedKeyword = r.SuggestedKeyword,
						SearchVolume = r.SearchVolume > 0 ? r.SearchVolume : 0,
						Difficulty = r.Difficulty > 0 ? r.Difficulty : 0,
						CPC = r.CPC > 0 ? r.CPC : 0m,
						CreatedDate = r.CreatedDate.ToString("yyyy-MM-dd"),
						MonthlySearchVolumes = r.MonthlySearchVolumes.Select(m => new
						{
							Month = m.Month,
							Year = m.Year,
							Searches = m.Searches
						}).ToList()
					})
					.DistinctBy(r => new { r.SeedKeyword, r.SuggestedKeyword })
					.ToList();

				var relatedKeywords = previousResults
					.Where(r => !r.IsMainKeyword)
					.GroupBy(r => r.SeedKeyword)
					.Select(g => new
					{
						SeedKeyword = g.Key,
						Keywords = g.Select(r => new
						{
							SuggestedKeyword = r.SuggestedKeyword,
							SearchVolume = r.SearchVolume > 0 ? r.SearchVolume : 0,
							Difficulty = r.Difficulty > 0 ? r.Difficulty : 0,
							CPC = r.CPC > 0 ? r.CPC : 0m,
							CreatedDate = r.CreatedDate.ToString("yyyy-MM-dd"),
							MonthlySearchVolumes = r.MonthlySearchVolumes.Select(m => new
							{
								Month = m.Month,
								Year = m.Year,
								Searches = m.Searches
							}).ToList()
						}).ToList()
					})
					.ToList();

				ViewBag.MainKeywords = mainKeywords;
				ViewBag.RelatedKeywords = relatedKeywords;

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
			}

			return View(new KeywordResearchViewModel());
		}


		[HttpPost]
		public async Task<IActionResult> IndexResearch(KeywordResearchViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.SeedKeyword) || model.ProjectId <= 0)
			{
				model.Message = "Vui lòng nhập từ khóa gốc và chọn ProjectId hợp lệ.";
				return View("Index", model);
			}


			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login", "Account");
			}

			if (!await _userService.CanPerformActionAsync(user.Id, ActionType.KeywordResearch.ToString()))
			{
				TempData["Error"] = "Bạn đã vượt quá giới hạn nghiên cứu từ khóa mỗi ngày.";
				return View("Index", model);
			}

			try
			{
				var suggestions = await _keywordResearchService.ResearchKeywordsAsync(model.ProjectId, model.SeedKeyword);

				var mainKeywords = suggestions
				.Where(s => s.IsMainKeyword)
				.Select(s => new
				{
					SeedKeyword = s.SeedKeyword,
					SuggestedKeyword = s.SuggestedKeyword,
					SearchVolume = s.SearchVolume ?? 0,
					Difficulty = s.Difficulty ?? 0,
					CPC = s.CPC ?? 0m,
					CreatedDate = s.CreatedDate.ToString("yyyy-MM-dd"),
					MonthlySearchVolumes = s.MonthlySearchVolumes.Select(m => new
					{
						Month = m.Month,
						Year = m.Year,
						Searches = m.Searches
					}).ToList()
				})
				.DistinctBy(s => new { s.SeedKeyword, s.SuggestedKeyword })
				.ToList();

				var relatedKeywords = suggestions
					.Where(s => !s.IsMainKeyword)
					.GroupBy(s => s.SeedKeyword)
					.Select(g => new
					{
						SeedKeyword = g.Key,
						Keywords = g.Select(s => new
						{
							SuggestedKeyword = s.SuggestedKeyword,
							SearchVolume = s.SearchVolume ?? 0,
							Difficulty = s.Difficulty ?? 0,
							CPC = s.CPC ?? 0m,
							CreatedDate = s.CreatedDate.ToString("yyyy-MM-dd"),
							MonthlySearchVolumes = s.MonthlySearchVolumes.Select(m => new
							{
								Month = m.Month,
								Year = m.Year,
								Searches = m.Searches
							}).ToList()
						}).ToList()
					})
					.ToList();

				ViewBag.MainKeywords = mainKeywords;
				ViewBag.RelatedKeywords = relatedKeywords;

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
					.FirstOrDefault(w => w.SeedKeyword == seedKeyword);
				if (suggestion == null)
				{
					TempData["Error"] = "Từ khóa không tồn tại trong dự án.";
					return RedirectToAction(nameof(Index), new { projectId });
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
