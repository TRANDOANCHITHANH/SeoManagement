using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class ScheduledKeywordCheckService
	{
		private readonly IService<Keyword> _keywordService;
		private readonly ISEOProjectService _projectService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IEmailSender _emailService;
		private readonly ILogger<ScheduledKeywordCheckService> _logger;

		public ScheduledKeywordCheckService(
			IService<Keyword> keywordService,
			ISEOProjectService projectService,
			UserManager<ApplicationUser> userManager,
			IEmailSender emailService,
			ILogger<ScheduledKeywordCheckService> logger)
		{
			_keywordService = keywordService;
			_projectService = projectService;
			_userManager = userManager;
			_emailService = emailService;
			_logger = logger;
		}

		public async Task CheckAndSendReportAsync()
		{
			try
			{
				_logger.LogInformation("Bắt đầu kiểm tra định kỳ thứ hạng từ khóa: {Time}", DateTime.Now);

				var projects = await _projectService.GetAllAsync("KeywordRankChecker");
				if (projects == null || !projects.Any())
				{
					_logger.LogWarning("Không có dự án nào để kiểm tra.");
					return;
				}

				foreach (var project in projects.Where(p => p.IsAutoReportEnabled == true))
				{
					var keywords = await _keywordService.GetByProjectIdAsync(project.ProjectID);
					if (keywords == null || !keywords.Any())
					{
						_logger.LogWarning("Dự án {ProjectId} không có từ khóa để kiểm tra.", project.ProjectID);
						continue;
					}

					// Kiểm tra thứ hạng cho từng từ khóa
					foreach (var keyword in keywords)
					{
						try
						{
							await ((KeywordService)_keywordService).GetAndSaveKeywordRankAsync(keyword.KeywordName, keyword.Domain, project.ProjectID);
							_logger.LogInformation("Cập nhật thành công từ khóa {Keyword} trên domain {Domain} trong dự án {ProjectId}.", keyword.KeywordName, keyword.Domain, project.ProjectID);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Lỗi khi kiểm tra từ khóa {Keyword} trên domain {Domain} trong dự án {ProjectId}.", keyword.KeywordName, keyword.Domain, project.ProjectID);
						}
					}

					// Lấy dữ liệu mới nhất để tạo báo cáo
					var updatedKeywords = await _keywordService.GetByProjectIdAsync(project.ProjectID);
					var results = updatedKeywords
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

					var user = await _userManager.FindByIdAsync(project.UserId.ToString());
					if (user == null || string.IsNullOrEmpty(user.Email))
					{
						_logger.LogWarning("Không tìm thấy email người dùng cho dự án {ProjectId}.", project.ProjectID);
						continue;
					}

					// Tạo báo cáo và gửi email
					var htmlBody = _emailService.GenerateHtmlReport(results, project.ProjectName);
					var excelStream = _emailService.GenerateExcelReport(results, project.ProjectID);
					var attachmentFileName = $"KeywordRankReport_Project_{project.ProjectID}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

					await _emailService.SendEmailDailyAsync(
						user.Email,
						$"Báo cáo thứ hạng từ khóa - Dự án {project.ProjectName}",
						htmlBody,
						excelStream,
						attachmentFileName);

					_logger.LogInformation("Đã gửi báo cáo cho dự án {ProjectId} tới email {Email}.", project.ProjectID, user.Email);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi thực hiện kiểm tra định kỳ và gửi báo cáo.");
			}
		}
	}
}
