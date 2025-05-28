using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_alertconfiguration_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IsAlertMonitored = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertConfigurations_SEOProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertConfigurations_ProjectId",
                table: "AlertConfigurations",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertConfigurations");
        }
    }
}
