using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_backlinks_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "Backlinks");

            migrationBuilder.AddColumn<int>(
                name: "DofollowBacklinks",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DofollowRefDomains",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DofollowBacklinks",
                table: "Backlinks");

            migrationBuilder.DropColumn(
                name: "DofollowRefDomains",
                table: "Backlinks");

            migrationBuilder.AddColumn<float>(
                name: "QualityScore",
                table: "Backlinks",
                type: "real",
                nullable: true);
        }
    }
}
