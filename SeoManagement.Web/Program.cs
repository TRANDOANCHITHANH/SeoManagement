using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;
using SeoManagement.Infrastructure.Repositories;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Utilities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

builder.Services.AddHangfire(config => config
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UseMemoryStorage());

builder.Services.AddHangfireServer();

builder.Services.Configure<IdentityOptions>(options =>
{
	// Password settings
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = true;
	options.Password.RequiredLength = 6;

	// Lockout settings
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
	options.Lockout.MaxFailedAccessAttempts = 5;
	options.Lockout.AllowedForNewUsers = true;

	// User settings
	options.User.RequireUniqueEmail = true;
});

builder.Services.AddAuthentication("DynamicAuth")
	.AddCookie("MainAuth", options =>
	{
		options.LoginPath = "/Account/Login";
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/AccessDenied";
	})
	.AddCookie("AdminAuth", options =>
	{
		options.LoginPath = "/Admin/Account/Login";
		options.LogoutPath = "/Admin/Account/Logout";
		options.AccessDeniedPath = "/Admin/Account/AccessDenied";
	})
	.AddPolicyScheme("DynamicAuth", "DynamicAuth", options =>
	{
		options.ForwardDefaultSelector = context =>
		{
			if (context.Request.Path.StartsWithSegments("/admin"))
			{
				return "AdminAuth";
			}
			return "MainAuth";
		};
	});


builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
	options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<GoogleSearchConsoleService>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<IApiServiceFactory, ApiServiceFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISEOProjectRepository, SEOProjectRepository>();
builder.Services.AddScoped<ISEOProjectService, SEOProjectService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ScheduledKeywordCheckService>();
builder.Services.AddScoped<ISEOOnPageCheckService, SEOOnPageCheckService>();
builder.Services.AddScoped<IKeywordRepository, KeywordRepository>();
builder.Services.AddScoped<IService<Keyword>, KeywordService>();
builder.Services.AddScoped<IIndexCheckerUrlRepository, IndexCheckerUrlRepository>();
builder.Services.AddScoped<IIndexCheckerUrlService, IndexCheckerUrlService>();
builder.Services.AddScoped<IPageSpeedResultRepository, PageSpeedResultRepository>();
builder.Services.AddScoped<IPageSpeedResultService, PageSpeedResultService>();
builder.Services.AddScoped<PageSpeedService>();
builder.Services.AddScoped<GoogleCustomSearchService>();
builder.Services.AddScoped<IBacklinkResultRepository, BacklinkResultRepository>();
builder.Services.AddScoped<IBacklinkResultService, BacklinkResultService>();
builder.Services.AddScoped<BacklinkService>();
builder.Services.AddScoped<IWebsiteInsightRepository, WebsiteInsightRepository>();
builder.Services.AddScoped<IService<WebsiteInsight>, WebsiteInsightService>();
builder.Services.AddScoped<IKeywordSuggestionRepository, KeywordSuggestionRepository>();
builder.Services.AddScoped<IKeywordResearchService, KeywordResearchService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", builder =>
	{
		builder.AllowAnyOrigin()
			   .AllowAnyMethod()
			   .AllowAnyHeader();
	});
});

builder.Host.UseSerilog((context, configuration) =>
{
	configuration
		.ReadFrom.Configuration(context.Configuration)
		.WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");
app.UseHangfireDashboard("/admin/hangfire", new DashboardOptions
{
	Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

app.MapControllerRoute(
	name: "areas",
	pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}"
);

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
	var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

	string[] roles = { "Admin", "User" };
	foreach (var role in roles)
	{
		if (!await roleManager.RoleExistsAsync(role))
		{
			await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
		}
	}
	var adminUser = await userManager.FindByEmailAsync("admin@example.com");
	if (adminUser == null)
	{
		adminUser = new ApplicationUser
		{
			UserName = "adminn",
			Email = "admin@example.com",
			FullName = "Admin User",
			CreatedDate = DateTime.UtcNow,
			IsActive = true
		};
		await userManager.CreateAsync(adminUser, "12345678aA@");
		await userManager.AddToRoleAsync(adminUser, "Admin");
	}
}

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("HangfireSetup");
var checkFrequency = builder.Configuration.GetSection("KeywordCheckSettings:Frequency").Value;

using (var scope = app.Services.CreateScope())
{
	var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
	var scheduledService = scope.ServiceProvider.GetRequiredService<ScheduledKeywordCheckService>();

	if (checkFrequency == "Daily")
	{
		recurringJobManager.AddOrUpdate(
			"CheckKeywordRanksDaily",
			() => scheduledService.CheckAndSendReportAsync(),
			Cron.Daily(0, 0));
		logger.LogInformation("Triggering daily keyword check job immediately for testing");
		recurringJobManager.Trigger("CheckKeywordRanksDaily");
	}
	else if (checkFrequency == "Weekly")
	{
		recurringJobManager.AddOrUpdate(
			"CheckKeywordRanksWeekly",
			() => scheduledService.CheckAndSendReportAsync(),
			Cron.Weekly(DayOfWeek.Monday, 0, 0));

		logger.LogInformation("Triggering weekly keyword check job immediately for testing");
		recurringJobManager.Trigger("CheckKeywordRanksWeekly");
	}
}

app.Run();
