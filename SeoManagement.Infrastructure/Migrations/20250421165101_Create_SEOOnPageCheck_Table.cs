using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create_SEOOnPageCheck_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SEOOnPageChecks",
                columns: table => new
                {
                    CheckID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MainKeyword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEOOnPageChecks", x => x.CheckID);
                    table.ForeignKey(
                        name: "FK_SEOOnPageChecks_SEOProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SEOOnPageChecks_ProjectID",
                table: "SEOOnPageChecks",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SEOOnPageChecks");
        }
    }
}
