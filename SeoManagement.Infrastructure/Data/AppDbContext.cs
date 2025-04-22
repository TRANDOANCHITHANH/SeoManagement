using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;

namespace SeoManagement.Infrastructure.Data
{
	public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		public DbSet<SEOProject> SEOProjects { get; set; }
		public DbSet<Keyword> Keywords { get; set; }
		public DbSet<KeywordGroup> KeywordGroups { get; set; }
		public DbSet<KeywordHistory> KeywordHistories { get; set; }
		public DbSet<Content> Contents { get; set; }
		public DbSet<ContentOutline> ContentOutlines { get; set; }
		public DbSet<RewrittenContent> RewrittenContents { get; set; }
		public DbSet<Backlink> Backlinks { get; set; }
		public DbSet<Report> Reports { get; set; }
		public DbSet<Prediction> Predictions { get; set; }
		public DbSet<Guide> Guides { get; set; }
		public DbSet<SystemConfig> SystemConfigs { get; set; }
		public DbSet<Site> Sites { get; set; }
		public DbSet<SEOOnPageCheck> SEOOnPageChecks { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Ánh xạ bảng Identity
			modelBuilder.Entity<ApplicationUser>(b =>
			{
				b.ToTable("Users");
				b.Property(u => u.Id).HasColumnName("UserId");

				// Mối quan hệ
				b.HasMany(u => u.SEOProjects)
					.WithOne(p => p.User)
					.HasForeignKey(p => p.UserId)
					.OnDelete(DeleteBehavior.Restrict);

				b.HasMany(u => u.Guides)
					.WithOne(g => g.User)
					.HasForeignKey(g => g.UserId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<IdentityRole<int>>(b =>
			{
				b.ToTable("Roles");
			});

			modelBuilder.Entity<IdentityUserRole<int>>(b =>
			{
				b.ToTable("UserRoles");
			});

			// Cấu hình mối quan hệ cho SEOProject
			modelBuilder.Entity<SEOProject>(entity =>
			{
				entity.HasMany(p => p.Keywords)
					.WithOne(k => k.Project)
					.HasForeignKey(k => k.ProjectID)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasMany(p => p.Contents)
					.WithOne(c => c.Project)
					.HasForeignKey(c => c.ProjectID)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasMany(p => p.Backlinks)
					.WithOne(b => b.Project)
					.HasForeignKey(b => b.ProjectID)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasMany(p => p.Reports)
					.WithOne(r => r.Project)
					.HasForeignKey(r => r.ProjectID)
					.OnDelete(DeleteBehavior.Cascade);

			});
			// Cấu hình mối quan hệ cho Content
			modelBuilder.Entity<Content>(entity =>
			{
				entity.HasMany(c => c.ContentOutlines)
					.WithOne(co => co.Content)
					.HasForeignKey(co => co.ContentID)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<SEOOnPageCheck>()
				.HasOne(c => c.Project)
				.WithMany(p => p.SEOOnPageChecks)
				.HasForeignKey(c => c.ProjectID);
		}
	}
}