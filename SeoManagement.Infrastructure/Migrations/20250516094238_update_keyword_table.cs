using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_keyword_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Competition",
                table: "Keywords");

            migrationBuilder.DropColumn(
                name: "SearchIntent",
                table: "Keywords");

            migrationBuilder.RenameColumn(
                name: "SearchVolume",
                table: "Keywords",
                newName: "TopVolume");

            migrationBuilder.RenameColumn(
                name: "CurrentRank",
                table: "Keywords",
                newName: "TopPosition");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Keywords",
                newName: "LastUpdate");

            migrationBuilder.AlterColumn<string>(
                name: "TopCountrySharesJson",
                table: "WebsiteInsights",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Keywords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerpResultsJson",
                table: "Keywords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Keywords");

            migrationBuilder.DropColumn(
                name: "SerpResultsJson",
                table: "Keywords");

            migrationBuilder.RenameColumn(
                name: "TopVolume",
                table: "Keywords",
                newName: "SearchVolume");

            migrationBuilder.RenameColumn(
                name: "TopPosition",
                table: "Keywords",
                newName: "CurrentRank");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "Keywords",
                newName: "CreatedDate");

            migrationBuilder.AlterColumn<string>(
                name: "TopCountrySharesJson",
                table: "WebsiteInsights",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<float>(
                name: "Competition",
                table: "Keywords",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchIntent",
                table: "Keywords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
