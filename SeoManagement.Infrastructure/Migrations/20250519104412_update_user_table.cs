using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class update_user_table : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "DailyKeywordCheckLimit",
				table: "Users",
				type: "int",
				nullable: true,
				defaultValue: 5);

			migrationBuilder.AddColumn<int>(
				name: "KeywordChecksToday",
				table: "Users",
				type: "int",
				nullable: true,
				defaultValue: 0);

			migrationBuilder.AddColumn<DateTime>(
				name: "LastCheckDate",
				table: "Users",
				type: "datetime2",
				nullable: true,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "DailyKeywordCheckLimit",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "KeywordChecksToday",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "LastCheckDate",
				table: "Users");
		}
	}
}
