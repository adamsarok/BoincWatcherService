using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostStats",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    HostName = table.Column<string>(type: "text", nullable: false),
                    TotalCredit = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostStats", x => new { x.YYYYMMDD, x.HostName });
                });

            migrationBuilder.CreateTable(
                name: "ProjectStats",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    TotalCredit = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
