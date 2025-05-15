using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class create_websiteinsight_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ReferringDomains",
                table: "Backlinks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DofollowRefDomains",
                table: "Backlinks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DofollowBacklinks",
                table: "Backlinks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "WebsiteInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GlobalVisits = table.Column<long>(type: "bigint", nullable: true),
                    BounceRate = table.Column<double>(type: "float", nullable: true),
                    PagesPerVisit = table.Column<double>(type: "float", nullable: true),
                    TimeOnSite = table.Column<double>(type: "float", nullable: true),
                    SearchTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    DirectTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    ReferralTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    SocialTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    PaidReferralTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    MailTrafficPercentage = table.Column<double>(type: "float", nullable: true),
                    TopKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GlobalRank = table.Column<long>(type: "bigint", nullable: true),
                    CountryRankCountry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryRankValue = table.Column<long>(type: "bigint", nullable: true),
                    CategoryRankCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryRankValue = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebsiteInsights_SEOProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "SEOProjects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteInsights_ProjectID",
                table: "WebsiteInsights",
                column: "ProjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebsiteInsights");

            migrationBuilder.AlterColumn<int>(
                name: "ReferringDomains",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DofollowRefDomains",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DofollowBacklinks",
                table: "Backlinks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
