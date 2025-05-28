using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Entities.Dtos;
using SeoManagement.Core.Enum;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class AlertService
	{
		private readonly IService<Keyword> _keywordService;
		private readonly IIndexCheckerUrlService _indexCheckerUrlService;
		private readonly IPageSpeedResultService _pageSpeedResultService;
		private readonly IBacklinkResultService _backlinkResultService;
		private readonly ISEOProjectService _seoProjectService;
		private readonly IEmailSender _emailSender;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<AlertService> _logger;

		public AlertService(
		   IService<Keyword> keywordService,
		   IIndexCheckerUrlService indexCheckerUrlService,
		   IPageSpeedResultService pageSpeedResultService,
		   IBacklinkResultService backlinkResultService,
		   ISEOProjectService seoProjectService,
		   IEmailSender emailSender,
		   UserManager<ApplicationUser> userManager,
		   ILogger<AlertService> logger)
		{
			_keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
			_indexCheckerUrlService = indexCheckerUrlService ?? throw new ArgumentNullException(nameof(indexCheckerUrlService));
			_pageSpeedResultService = pageSpeedResultService ?? throw new ArgumentNullException(nameof(pageSpeedResultService));
			_backlinkResultService = backlinkResultService ?? throw new ArgumentNullException(nameof(backlinkResultService));
			_seoProjectService = seoProjectService ?? throw new ArgumentNullException(nameof(seoProjectService));
			_emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
			_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<List<Alert>> CheckAlertsAsync(int userId, bool sendEmail = false)
		{
			var alerts = new List<Alert>();
			var projectAlerts = new Dictionary<int, List<Alert>>();

			var projects = (await _seoProjectService.GetPagedAsync(1, int.MaxValue, userId)).Items
			   .Where(p => p.IsMonitored == true && p.Status == (int)ProjectStatus.Active && p.AlertConfiguration != null && p.AlertConfiguration.IsAlertMonitored)
			   .ToList();
			if (projects == null || !projects.Any())
			{
				_logger.LogInformation("Người dùng {UserId} không có dự án nào được bật theo dõi hoặc cảnh báo.", userId);
				return alerts;
			}

			var projectTypes = new[] { "KeywordRankChecker", "IndexChecker", "PageSpeedChecker", "BacklinkChecker" };
			var monitoredTypes = projects.Select(p => p.ProjectType).Distinct().ToList();
			if (monitoredTypes.Count != 4 || !projectTypes.All(t => monitoredTypes.Contains(t)))
			{
				_logger.LogWarning("Người dùng {UserId} chưa chọn đủ 4 loại dự án để theo dõi.", userId);
				return alerts;
			}

			foreach (var project in projects)
			{
				int projectId = project.ProjectID;

				// 1. Kiểm tra từ khóa tụt hạng
				var keywords = await _keywordService.GetByProjectIdAsync(projectId);
				foreach (var keyword in keywords)
				{
					var histories = keyword.KeywordHistories?.OrderByDescending(h => h.RecordedDate).Take(2).ToList();
					if (histories != null && histories.Count == 2)
					{
						var latestRank = histories[0].Rank;
						var previousRank = histories[1].Rank;
						if (latestRank > previousRank && latestRank > 10)
						{
							var alert = new Alert
							{
								Type = AlertType.KeywordDrop,
								Message = $"Từ khóa '{keyword.KeywordName}' tụt từ vị trí {previousRank} xuống {latestRank}.",
								EntityId = keyword.KeywordID,
								ProjectId = projectId,
								Timestamp = DateTime.UtcNow
							};
							alerts.Add(alert);
							AddToProjectAlerts(projectAlerts, projectId, alert);
						}
					}
				}

				// 2. Kiểm tra URL không được lập chỉ mục
				var indexUrls = await _indexCheckerUrlService.GetByProjectIdAsync(projectId);
				foreach (var url in indexUrls)
				{
					if (url.IsIndexed == false || !string.IsNullOrEmpty(url.ErrorMessage))
					{
						var alert = new Alert
						{
							Type = AlertType.IndexingIssue,
							Message = $"URL '{url.Url}' không được lập chỉ mục: {(string.IsNullOrEmpty(url.ErrorMessage) ? "Not Indexed" : url.ErrorMessage)}.",
							EntityId = url.UrlID,
							ProjectId = projectId,
							Timestamp = DateTime.UtcNow
						};
						alerts.Add(alert);
						AddToProjectAlerts(projectAlerts, projectId, alert);
					}
				}

				// 3. Kiểm tra tốc độ trang giảm
				var pageSpeedResults = await _pageSpeedResultService.GetByProjectIdAsync(projectId);
				var groupedResults = pageSpeedResults.GroupBy(r => r.Url);
				foreach (var group in groupedResults)
				{
					var orderedResults = group.OrderByDescending(r => r.LastCheckedDate).Take(2).ToList();
					if (orderedResults.Count == 2)
					{
						var latest = orderedResults[0];
						var previous = orderedResults[1];
						if (latest.LoadTime.HasValue && previous.LoadTime.HasValue && latest.LoadTime > previous.LoadTime + 0.5)
						{
							var alert = new Alert
							{
								Type = AlertType.PageSpeedDrop,
								Message = $"Tốc độ trang '{latest.Url}' giảm: thời gian tải tăng từ {previous.LoadTime.Value:F2}s lên {latest.LoadTime.Value:F2}s.",
								EntityId = latest.Id,
								ProjectId = projectId,
								Timestamp = DateTime.UtcNow
							};
							alerts.Add(alert);
							AddToProjectAlerts(projectAlerts, projectId, alert);
						}
					}
				}

				// 4. Kiểm tra backlink xấu
				var backlinks = await _backlinkResultService.GetByProjectIdAsync(projectId);
				foreach (var backlink in backlinks)
				{
					if (backlink.ReferringDomains < 5 || (backlink.DofollowBacklinks.HasValue && backlink.DofollowBacklinks < 2))
					{
						var alert = new Alert
						{
							Type = AlertType.BadBacklink,
							Message = $"Backlink từ '{backlink.Url}' có chất lượng thấp: {backlink.ReferringDomains} referring domains, {backlink.DofollowBacklinks ?? 0} dofollow backlinks.",
							EntityId = backlink.BacklinkID,
							ProjectId = projectId,
							Timestamp = DateTime.UtcNow
						};
						alerts.Add(alert);
						AddToProjectAlerts(projectAlerts, projectId, alert);
					}
				}
			}

			if (sendEmail && projectAlerts.Any())
			{
				foreach (var project in projectAlerts)
				{
					var projectId = project.Key;
					var projectAlertsList = project.Value;
					var projectEntity = await _seoProjectService.GetByIdAsync(projectId);

					if (projectEntity == null)
					{
						_logger.LogWarning("Không tìm thấy dự án {ProjectId} để gửi cảnh báo.", projectId);
						continue;
					}

					var user = await _userManager.FindByIdAsync(projectEntity.UserId.ToString());
					if (user == null || string.IsNullOrEmpty(user.Email))
					{
						_logger.LogWarning("Không tìm thấy email người dùng cho dự án {ProjectId}.", projectId);
						continue;
					}

					var subject = $"Cảnh báo SEO - Dự án {projectEntity.ProjectName}";
					var body = GenerateHtmlAlertBody(projectAlertsList, projectEntity.ProjectName);
					await _emailSender.SendEmailAsync(user.Email, subject, body);
					_logger.LogInformation("Đã gửi email cảnh báo cho {Email} với {AlertCount} cảnh báo.", user.Email, projectAlertsList.Count);
				}
			}

			return alerts;
		}
		public async Task<List<Alert>> CheckAlertsWithoutEmailAsync(int userId)
		{
			return await CheckAlertsAsync(userId, sendEmail: false);
		}

		public async Task CheckAlertsForProjectsAsync(int userId)
		{
			var projects = (await _seoProjectService.GetPagedAsync(1, int.MaxValue, userId)).Items
			   .Where(p => p.IsMonitored == true && p.Status == (int)ProjectStatus.Active && p.AlertConfiguration != null && p.AlertConfiguration.IsAlertMonitored)
			   .ToList();

			if (projects == null || !projects.Any())
			{
				_logger.LogInformation("Không có dự án nào được bật theo dõi cảnh báo cho người dùng {UserId}.", userId);
				return;
			}

			var projectAlerts = new Dictionary<int, List<Alert>>();
			foreach (var project in projects)
			{
				var alerts = await CheckAlertsAsync(project.UserId, sendEmail: false);
				foreach (var alert in alerts)
				{
					AddToProjectAlerts(projectAlerts, project.ProjectID, alert);
				}
			}

			if (projectAlerts.Any())
			{
				foreach (var project in projectAlerts)
				{
					var projectId = project.Key;
					var projectAlertsList = project.Value;
					var projectEntity = await _seoProjectService.GetByIdAsync(projectId);

					if (projectEntity == null)
					{
						_logger.LogWarning("Không tìm thấy dự án {ProjectId} để gửi cảnh báo.", projectId);
						continue;
					}

					var user = await _userManager.FindByIdAsync(projectEntity.UserId.ToString());
					if (user == null || string.IsNullOrEmpty(user.Email))
					{
						_logger.LogWarning("Không tìm thấy email người dùng cho dự án {ProjectId}.", projectId);
						continue;
					}

					var subject = $"Cảnh báo SEO - Dự án {projectEntity.ProjectName}";
					var body = GenerateHtmlAlertBody(projectAlertsList, projectEntity.ProjectName);
					await _emailSender.SendEmailAsync(user.Email, subject, body);
					_logger.LogInformation("Đã gửi email cảnh báo tự động cho {Email} với {AlertCount} cảnh báo.", user.Email, projectAlertsList.Count);
				}
			}
		}

		private void AddToProjectAlerts(Dictionary<int, List<Alert>> projectAlerts, int projectId, Alert alert)
		{
			if (!projectAlerts.ContainsKey(projectId))
			{
				projectAlerts[projectId] = new List<Alert>();
			}
			projectAlerts[projectId].Add(alert);
		}

		private string GenerateHtmlAlertBody(List<Alert> alerts, string projectName)
		{
			var html = $"<h2>Cảnh báo SEO cho dự án {projectName}</h2>";
			html += "<ul>";
			foreach (var alert in alerts)
			{
				html += $"<li><strong>{alert.Type}</strong>: {alert.Message} (Thời gian: {alert.Timestamp:yyyy-MM-dd HH:mm:ss})</li>";
			}
			html += "</ul>";
			html += "<p>Vui lòng kiểm tra và thực hiện hành động cần thiết.</p>";
			return html;
		}
	}
}
