using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincStatsFunctionApp.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostStats",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalCredit = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostStats", x => new { x.YYYYMMDD, x.HostName });
                });

            migrationBuilder.CreateTable(
                name: "ProjectStats",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalCredit = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStats", x => new { x.YYYYMMDD, x.ProjectName });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostStats");

            migrationBuilder.DropTable(
                name: "ProjectStats");
        }
    }
}
