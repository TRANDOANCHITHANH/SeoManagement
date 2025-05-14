using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_PageSpeedResult_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PageSpeedResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoadTime = table.Column<double>(type: "float", nullable: false),
                    LCP = table.Column<double>(type: "float", nullable: true),
                    FID = table.Column<double>(type: "float", nullable: true),
                    CLS = table.Column<double>(type: "float", nullable: true),
                    Suggestions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastCheckedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSpeedResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageSpeedResults_SEOProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageSpeedResults_ProjectID",
                table: "PageSpeedResults",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageSpeedResults");
        }
    }
}
