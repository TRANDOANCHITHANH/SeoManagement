using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_keywordsuggestion_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeywordSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    SeedKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMainKeyword = table.Column<bool>(type: "bit", nullable: false),
                    SearchVolume = table.Column<int>(type: "int", nullable: true),
                    Difficulty = table.Column<int>(type: "int", nullable: true),
                    CPC = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeywordSuggestions_SEOProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlySearchVolumes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeywordSuggestionId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Searches = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySearchVolumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlySearchVolumes_KeywordSuggestions_KeywordSuggestionId",
                        column: x => x.KeywordSuggestionId,
                        principalTable: "KeywordSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordSuggestions_ProjectID",
                table: "KeywordSuggestions",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySearchVolumes_KeywordSuggestionId",
                table: "MonthlySearchVolumes",
                column: "KeywordSuggestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlySearchVolumes");

            migrationBuilder.DropTable(
                name: "KeywordSuggestions");
        }
    }
}
