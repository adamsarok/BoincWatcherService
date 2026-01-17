using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class ProjectMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectStats");

            migrationBuilder.CreateTable(
                name: "ProjectMaps",
                columns: table => new
                {
                    MasterUrl = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMaps", x => x.MasterUrl);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStats2",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterUrl = table.Column<string>(type: "text", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    TotalCredit = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStats2", x => new { x.YYYYMMDD, x.ProjectId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMaps");

            migrationBuilder.DropTable(
                name: "ProjectStats2");

            migrationBuilder.CreateTable(
                name: "ProjectStats",
                columns: table => new
                {
                    YYYYMMDD = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    LatestTaskDownloadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalCredit = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStats", x => new { x.YYYYMMDD, x.ProjectName });
                });
        }
    }
}
