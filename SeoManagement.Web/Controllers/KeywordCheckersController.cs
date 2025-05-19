using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Enum;
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
		private readonly IUserService _userService;
		public KeywordCheckersController(
			IService<Keyword> keywordService,
			ISEOProjectService projectService,
			ILogger<KeywordCheckersController> logger,
			UserManager<ApplicationUser> userManager,
			IUserService userService)
		{
			_keywordService = keywordService;
			_projectService = projectService;
			_logger = logger;
			_userManager = userManager;
			_userService = userService;
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
						CurrentPosition = r.TopPosition.HasValue ? r.TopPosition.Value : -1,
						PreviousPosition = r.KeywordHistories != null && r.KeywordHistories.Any()
							? r.KeywordHistories.OrderByDescending(h => h.RecordedDate).Skip(1).FirstOrDefault()?.Rank ?? -1
							: -1,
						BestPosition = r.KeywordHistories != null
							? new List<int> { r.TopPosition ?? -1 }.Concat(r.KeywordHistories.Select(h => h.Rank))
								.Where(p => p > 0)
								.DefaultIfEmpty(-1)
								.Min()
							: (r.TopPosition.HasValue && r.TopPosition.Value > 0 ? r.TopPosition.Value : -1),
						TopVolume = r.TopVolume ?? 0,
						LastUpdate = r.LastUpdate.ToString("yyyy-MM-dd"),
						SerpResultsJson = r.SerpResultsJson
					})
					.DistinctBy(r => new { r.Keyword, r.Domain })
					.ToList();

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
				ViewBag.IsAutoReportEnabled = project.IsAutoReportEnabled;
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
				if (!await _userService.CanPerformActionAsync(user.Id, ActionType.KeywordRankChecker.ToString()))
				{
					TempData["Error"] = "Bạn đã vượt quá giới hạn kiểm tra thứ hạng từ khóa mỗi ngày!";
					return RedirectToAction("IndexChecker", new { projectId = model.ProjectId });
				}


				var keyword = await ((KeywordService)_keywordService).GetAndSaveKeywordRankAsync(model.Keyword, model.Domain, model.ProjectId);

				model.Rank = new Keyword { KeywordID = keyword.KeywordID, KeywordName = keyword.KeywordName, Domain = keyword.Domain, TopPosition = keyword.TopPosition, TopVolume = keyword.TopVolume, LastUpdate = keyword.LastUpdate };
				model.Message = "Kiểm tra thứ hạng thành công!";
				await _userService.IncrementActionCountAsync(user.Id, ActionType.KeywordRankChecker.ToString());
				var allResults = await _keywordService.GetByProjectIdAsync(model.ProjectId);
				ViewBag.Results = allResults
					.Select(r => new
					{
						Keyword = r.KeywordName,
						Domain = r.Domain,
						CurrentPosition = r.TopPosition.HasValue ? r.TopPosition.Value : -1,
						PreviousPosition = r.KeywordHistories != null && r.KeywordHistories.Any()
							? r.KeywordHistories.OrderByDescending(h => h.RecordedDate).Skip(1).FirstOrDefault()?.Rank ?? -1
							: -1,
						BestPosition = r.KeywordHistories != null
							? new List<int> { r.TopPosition ?? -1 }.Concat(r.KeywordHistories.Select(h => h.Rank))
								.Where(p => p > 0)
								.DefaultIfEmpty(-1)
								.Min()
							: (r.TopPosition.HasValue && r.TopPosition.Value > 0 ? r.TopPosition.Value : -1),
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

		[HttpGet]
		public async Task<IActionResult> ExportToExcel(int projectId)
		{
			try
			{
				var keywords = await _keywordService.GetByProjectIdAsync(projectId);
				if (keywords == null || !keywords.Any())
				{
					TempData["Error"] = "Không có dữ liệu từ khóa để xuất.";
					return RedirectToAction(nameof(IndexChecker), new { projectId });
				}

				var results = keywords
					.Select(r =>
					{
						var previousPosition = r.KeywordHistories != null && r.KeywordHistories.Any()
							? r.KeywordHistories.OrderByDescending(h => h.RecordedDate).Skip(1).FirstOrDefault()?.Rank ?? -1
							: -1;

						return new
						{
							Keyword = r.KeywordName ?? "Unknown Keyword",
							Domain = r.Domain ?? "Unknown Domain",
							CurrentPosition = r.TopPosition.HasValue ? r.TopPosition.Value : -1,
							PreviousPosition = previousPosition,
							BestPosition = r.KeywordHistories != null
								? new List<int> { r.TopPosition ?? -1 }.Concat(r.KeywordHistories.Select(h => h.Rank))
									.Where(p => p > 0)
									.DefaultIfEmpty(-1)
									.Min()
								: (r.TopPosition.HasValue && r.TopPosition.Value > 0 ? r.TopPosition.Value : -1),
							TopVolume = r.TopVolume ?? 0,
							LastUpdate = r.LastUpdate
						};
					})
					.DistinctBy(r => new { r.Keyword, r.Domain })
					.ToList();

				using (var workbook = new XLWorkbook())
				{
					var worksheet = workbook.Worksheets.Add("KeywordRankReport");
					var currentRow = 1;

					// Tiêu đề cột
					worksheet.Cell(currentRow, 1).Value = "Từ khóa";
					worksheet.Cell(currentRow, 2).Value = "Domain";
					worksheet.Cell(currentRow, 3).Value = "Vị trí hiện tại";
					worksheet.Cell(currentRow, 4).Value = "Vị trí cũ";
					worksheet.Cell(currentRow, 5).Value = "Vị trí cao nhất";
					worksheet.Cell(currentRow, 6).Value = "Top Volume";
					worksheet.Cell(currentRow, 7).Value = "Ngày cập nhật cuối";
					worksheet.Range(currentRow, 1, currentRow, 7).Style.Font.Bold = true;

					currentRow++;
					foreach (var result in results)
					{
						worksheet.Cell(currentRow, 1).Value = result.Keyword;
						worksheet.Cell(currentRow, 2).Value = result.Domain;
						worksheet.Cell(currentRow, 3).Value = result.CurrentPosition > 0 ? result.CurrentPosition.ToString() : "N/A";
						worksheet.Cell(currentRow, 4).Value = result.PreviousPosition > 0 ? result.PreviousPosition.ToString() : "N/A";
						worksheet.Cell(currentRow, 5).Value = result.BestPosition > 0 ? result.BestPosition.ToString() : "N/A";
						worksheet.Cell(currentRow, 6).Value = result.TopVolume > 0 ? result.TopVolume.ToString() : "N/A";
						worksheet.Cell(currentRow, 7).Value = result.LastUpdate.ToString("dd/MM/yyyy HH:mm:ss");

						currentRow++;
					}

					worksheet.Columns().AdjustToContents();

					using (var stream = new MemoryStream())
					{
						workbook.SaveAs(stream);
						var content = stream.ToArray();
						return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KeywordRank_Project_{projectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xuất dữ liệu Excel cho dự án: {ProjectId}", projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xuất dữ liệu: " + ex.Message;
				return RedirectToAction(nameof(IndexChecker), new { projectId });
			}
		}

		[HttpPost]
		public async Task<IActionResult> AutoReport(int projectId, bool isEnabled)
		{
			var project = await _projectService.GetByIdAsync(projectId);
			if (project == null)
			{
				TempData["Error"] = "Dự án không tồn tại.";
				return RedirectToAction("IndexChecker", new { projectId });
			}

			project.IsAutoReportEnabled = isEnabled;
			await _projectService.UpdateSEOProjectAsync(project);
			TempData["Success"] = isEnabled ? "Đã bật tự động kiểm tra và gửi báo cáo." : "Đã tắt tự động kiểm tra và gửi báo cáo.";
			return RedirectToAction("IndexChecker", new { projectId });
		}
	}
}
