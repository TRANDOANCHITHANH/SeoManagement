using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;
using SeoManagement.Infrastructure.Repositories;
using SeoManagement.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISEOProjectRepository, SEOProjectRepository>();
builder.Services.AddScoped<ISEOProjectService, SEOProjectService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ISEOOnPageCheckService, SEOOnPageCheckService>();
builder.Services.AddScoped<IKeywordRepository, KeywordRepository>();
builder.Services.AddScoped<IKeywordService, KeywordService>();
builder.Services.AddHttpClient<GoogleCustomSearchService>();
builder.Services.AddScoped<IIndexCheckerUrlRepository, IndexCheckerUrlRepository>();
builder.Services.AddScoped<IIndexCheckerUrlService, IndexCheckerUrlService>();
builder.Services.AddScoped<IPageSpeedResultRepository, PageSpeedResultRepository>();
builder.Services.AddScoped<IPageSpeedResultService, PageSpeedResultService>();
builder.Services.AddScoped<GoogleCustomSearchService>(sp =>
{
	var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
	var configuration = sp.GetRequiredService<IConfiguration>();
	var apiKey = configuration["GoogleCustomSearch:ApiKey"];
	var searchEngineId = configuration["GoogleCustomSearch:SearchEngineId"];
	return new GoogleCustomSearchService(httpClient, apiKey, searchEngineId);
});

builder.Services.AddScoped<PageSpeedService>(sp =>
{
	var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
	var configuration = sp.GetRequiredService<IConfiguration>();
	var apiKey = configuration["GooglePageSpeed:ApiKey"];
	return new PageSpeedService(httpClient, apiKey);
});

builder.Services.AddScoped<IBacklinkResultRepository, BacklinkResultRepository>();
builder.Services.AddScoped<IBacklinkResultService, BacklinkResultService>();
builder.Services.AddScoped<BacklinkService>(sp =>
{
	var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
	var configuration = sp.GetRequiredService<IConfiguration>();
	var apiKey = configuration["RapidApiKey"];
	return new BacklinkService(httpClient, apiKey);
});
builder.Services.AddScoped<IWebsiteInsightRepository, WebsiteInsightRepository>();
builder.Services.AddScoped<IService<WebsiteInsight>, WebsiteInsightService>();
builder.Services.AddHttpClient<IService<WebsiteInsight>, WebsiteInsightService>();


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
app.Run();
