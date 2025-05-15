using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_backlink_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Backlinks");

            migrationBuilder.DropColumn(
                name: "SourceURL",
                table: "Backlinks");

            migrationBuilder.RenameColumn(
                name: "TargetURL",
                table: "Backlinks",
                newName: "Url");

            migrationBuilder.AddColumn<string>(
                name: "BacklinksDetails",
                table: "Backlinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedDate",
                table: "Backlinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferringDomains",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalBacklinks",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BacklinksDetails",
                table: "Backlinks");

            migrationBuilder.DropColumn(
                name: "LastCheckedDate",
                table: "Backlinks");

            migrationBuilder.DropColumn(
                name: "ReferringDomains",
                table: "Backlinks");

            migrationBuilder.DropColumn(
                name: "TotalBacklinks",
                table: "Backlinks");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Backlinks",
                newName: "TargetURL");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Backlinks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SourceURL",
                table: "Backlinks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
