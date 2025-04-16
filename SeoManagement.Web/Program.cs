using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;
using SeoManagement.Infrastructure.Repositories;
using SeoManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
	options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
	options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
}).AddCookie(options =>
	{
		options.LoginPath = "/Account/Login";
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/AccessDenied";
	});
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
	options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISEOProjectRepository, SEOProjectRepository>();
builder.Services.AddScoped<ISEOProjectService, SEOProjectService>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", builder =>
	{
		builder.AllowAnyOrigin()
			   .AllowAnyMethod()
			   .AllowAnyHeader();
	});
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

app.UseAuthorization();
app.UseCors("AllowAll");
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
