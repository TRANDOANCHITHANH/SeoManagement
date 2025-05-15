using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class update_field_table : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "IsDataFromGa",
				table: "WebsiteInsights",
				type: "bit",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "TopCountrySharesJson",
				table: "WebsiteInsights",
				type: "nvarchar(max)",
				nullable: true,
				defaultValue: "");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "IsDataFromGa",
				table: "WebsiteInsights");

			migrationBuilder.DropColumn(
				name: "TopCountrySharesJson",
				table: "WebsiteInsights");
		}
	}
}
