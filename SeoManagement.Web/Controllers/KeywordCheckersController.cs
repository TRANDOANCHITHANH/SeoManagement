using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class KeywordCheckersController : Controller
	{
		private readonly IService<Keyword> _keywordService;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<KeywordCheckersController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;

		public KeywordCheckersController(
			IService<Keyword> keywordService,
			ISEOProjectService projectService,
			ILogger<KeywordCheckersController> logger,
			UserManager<ApplicationUser> userManager)
		{
			_keywordService = keywordService;
			_projectService = projectService;
			_logger = logger;
			_userManager = userManager;
		}

		[HttpGet]
		public async Task<IActionResult> IndexChecker(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var previousResults = await _keywordService.GetByProjectIdAsync(projectId.Value);
				ViewBag.Results = previousResults
					.Select(r => new
					{
						Keyword = r.KeywordName,
						Domain = r.Domain,
						TopPosition = r.TopPosition ?? 0,
						TopVolume = r.TopVolume ?? 0,
						LastUpdate = r.LastUpdate.ToString("yyyy-MM-dd"),
						SerpResultsJson = r.SerpResultsJson
					})
					.ToList();

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
			}

			return View(new KeywordRankViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> IndexChecker(KeywordRankViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.Keyword) || string.IsNullOrWhiteSpace(model.Domain) || model.ProjectId <= 0)
			{
				model.Message = "Vui lòng nhập từ khóa, domain và chọn ProjectId hợp lệ.";
				return View(model);
			}

			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var keyword = await ((KeywordService)_keywordService).GetAndSaveKeywordRankAsync(model.Keyword, model.Domain, model.ProjectId);

				model.Rank = new Keyword { KeywordID = keyword.KeywordID, KeywordName = keyword.KeywordName, Domain = keyword.Domain, TopPosition = keyword.TopPosition, TopVolume = keyword.TopVolume, LastUpdate = keyword.LastUpdate }; // Map to Rank
				model.Message = "Kiểm tra thứ hạng thành công!";

				var allResults = await _keywordService.GetByProjectIdAsync(model.ProjectId);
				ViewBag.Results = allResults
					.Select(r => new
					{
						Keyword = r.KeywordName,
						Domain = r.Domain,
						TopPosition = r.TopPosition ?? 0,
						TopVolume = r.TopVolume ?? 0,
						LastUpdate = r.LastUpdate.ToString("yyyy-MM-dd"),
						SerpResultsJson = r.SerpResultsJson
					})
					.DistinctBy(r => new { r.Keyword, r.Domain })
					.ToList();

				var project = await _projectService.GetByIdAsync(model.ProjectId);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
				ViewBag.ProjectId = model.ProjectId;

				TempData["Success"] = "Kiểm tra thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi kiểm tra từ khóa {Keyword} trên domain {Domain}", model.Keyword, model.Domain);
				model.Message = $"Lỗi: {ex.Message}";
				TempData["Error"] = $"Đã xảy ra lỗi khi kiểm tra từ khóa: {ex.Message}";
			}

			return RedirectToAction("IndexChecker", new { model.ProjectId });
		}

		[HttpPost]
		public async Task<IActionResult> DeleteRank(int projectId, string keyword, string domain)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var keywordEntity = (await _keywordService.GetByProjectIdAsync(projectId))
					.FirstOrDefault(r => r.KeywordName == keyword && r.Domain == domain);
				if (keywordEntity == null)
				{
					TempData["Error"] = "Kết quả từ khóa không tồn tại trong dự án.";
					return RedirectToAction(nameof(Index), new { projectId });
				}

				await _keywordService.DeleteAsync(keywordEntity.KeywordID);

				TempData["Success"] = "Xóa kết quả thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa kết quả từ khóa {Keyword} trên domain {Domain} trong dự án: {ProjectId}", keyword, domain, projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xóa kết quả: " + ex.Message;
			}

			return RedirectToAction(nameof(IndexChecker), new { projectId });
		}
	}
}
