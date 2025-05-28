using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class UserAlertScheduler : BackgroundService
	{
		private readonly ILogger<UserAlertScheduler> _logger;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IRecurringJobManager _recurringJobManager;

		public UserAlertScheduler(
			ILogger<UserAlertScheduler> logger,
			IServiceScopeFactory serviceScopeFactory,
			IRecurringJobManager recurringJobManager)
		{
			_logger = logger;
			_serviceScopeFactory = serviceScopeFactory;
			_recurringJobManager = recurringJobManager;
			_logger.LogInformation("UserAlertScheduler initialized.");
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("UserAlertScheduler started.");
			_recurringJobManager.AddOrUpdate(
				"SendAlertsDaily_AllUsers",
				() => ProcessAlertsForAllUsers(),
				Cron.Daily(0, 0)); // Chạy lúc 0:00 hàng ngày

			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
			}
			_logger.LogInformation("UserAlertScheduler stopped.");
		}

		public async Task ProcessAlertsForAllUsers()
		{
			using (var scope = _serviceScopeFactory.CreateScope())
			{
				var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
				var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();

				int pageNumber = 1;
				int pageSize = 100;
				bool hasMoreUsers = true;

				while (hasMoreUsers)
				{
					var (users, totalItems) = await userService.GetPagedAsync(pageNumber, pageSize);
					if (users == null || !users.Any())
					{
						hasMoreUsers = false;
						break;
					}
					foreach (var user in users)
					{
						await alertService.CheckAlertsForProjectsAsync(user.Id);
					}

					if (pageNumber * pageSize >= totalItems)
					{
						hasMoreUsers = false;
					}
					else
					{
						pageNumber++;
					}
				}
			}
		}
	}
}
