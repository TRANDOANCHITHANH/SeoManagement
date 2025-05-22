using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_seoperformance_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SEOPerformanceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AverageKeywordRank = table.Column<double>(type: "float", nullable: true),
                    AverageOnPageScore = table.Column<double>(type: "float", nullable: true),
                    PageSpeedScore = table.Column<double>(type: "float", nullable: true),
                    BacklinkCount = table.Column<int>(type: "int", nullable: true),
                    IndexedPageCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEOPerformanceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SEOPerformanceHistories_SEOProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SEOPerformanceHistories_ProjectId",
                table: "SEOPerformanceHistories",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SEOPerformanceHistories");
        }
    }
}
