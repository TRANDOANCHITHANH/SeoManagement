using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_seoonpagecheck_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "SEOOnPageChecks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "SEOOnPageChecks");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "SEOOnPageChecks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "SEOOnPageChecks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SEOOnPageChecks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "SEOOnPageChecks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
