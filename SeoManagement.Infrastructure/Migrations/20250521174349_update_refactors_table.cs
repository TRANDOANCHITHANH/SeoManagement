using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class update_refactors_table : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "SeedKeywords",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					ProjectID = table.Column<int>(type: "int", nullable: false),
					Keyword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
					SearchVolume = table.Column<int>(type: "int", nullable: true),
					Difficulty = table.Column<int>(type: "int", nullable: true),
					CPC = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
					CompetitionValue = table.Column<string>(type: "nvarchar(50)", nullable: false),
					CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
					MonthlySearchVolumesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_SeedKeywords", x => x.Id);
					table.ForeignKey(
						name: "FK_SeedKeywords_SEOProjects_ProjectID",
						column: x => x.ProjectID,
						principalTable: "SEOProjects",
						principalColumn: "ProjectID",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RelatedKeywords",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					SeedKeywordId = table.Column<int>(type: "int", nullable: false),
					SuggestedKeyword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
					SearchVolume = table.Column<int>(type: "int", nullable: true),
					Difficulty = table.Column<int>(type: "int", nullable: true),
					CPC = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
					CompetitionValue = table.Column<string>(type: "nvarchar(50)", nullable: false),
					CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
					MonthlySearchVolumesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RelatedKeywords", x => x.Id);
					table.ForeignKey(
						name: "FK_RelatedKeywords_SeedKeywords_SeedKeywordId",
						column: x => x.SeedKeywordId,
						principalTable: "SeedKeywords",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_RelatedKeywords_SeedKeywordId",
				table: "RelatedKeywords",
				column: "SeedKeywordId");

			migrationBuilder.CreateIndex(
				name: "IX_SeedKeywords_ProjectID",
				table: "SeedKeywords",
				column: "ProjectID");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "RelatedKeywords");

			migrationBuilder.DropTable(
				name: "SeedKeywords");

		}
	}
}
