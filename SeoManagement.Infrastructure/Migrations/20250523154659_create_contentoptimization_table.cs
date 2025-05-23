using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_contentoptimization_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentOptimizationAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    TargetKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeywordUsage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeywordDensity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedKeywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AltAttributeIssues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageSuggestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleIssues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaSuggestions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordCount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReadabilityScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToneOfVoice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalityCheck = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentStructureIssues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkIssues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentOptimizationAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentOptimizationAnalyses_SEOProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentOptimizationAnalyses_ProjectID",
                table: "ContentOptimizationAnalyses",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentOptimizationAnalyses");
        }
    }
}
