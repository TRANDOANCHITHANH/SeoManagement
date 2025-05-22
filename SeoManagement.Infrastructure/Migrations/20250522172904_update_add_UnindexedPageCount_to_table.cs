using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class update_add_UnindexedPageCount_to_table : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "UnindexedPageCount",
				table: "SEOPerformanceHistories",
				type: "int",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "UnindexedPageCount",
				table: "SEOPerformanceHistories");
		}
	}
}
