using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class create_useractionlimit_table : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
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

			migrationBuilder.CreateTable(
				name: "UserActionLimits",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					UserId = table.Column<int>(type: "int", nullable: false),
					ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
					DailyLimit = table.Column<int>(type: "int", nullable: false),
					ActionsToday = table.Column<int>(type: "int", nullable: false),
					LastActionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_UserActionLimits", x => x.Id);
					table.ForeignKey(
						name: "FK_UserActionLimits_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_UserActionLimits_UserId",
				table: "UserActionLimits",
				column: "UserId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "UserActionLimits");

			migrationBuilder.AddColumn<int>(
				name: "DailyKeywordCheckLimit",
				table: "Users",
				type: "int",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "KeywordChecksToday",
				table: "Users",
				type: "int",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<DateTime>(
				name: "LastCheckDate",
				table: "Users",
				type: "datetime2",
				nullable: false,
				defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
		}
	}
}
