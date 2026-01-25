using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class BoincApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppName",
                table: "BoincTasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoincApps",
                columns: table => new
                {
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProjectUrl = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoincApps", x => new { x.ProjectName, x.Name });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoincApps");

            migrationBuilder.DropColumn(
                name: "AppName",
                table: "BoincTasks");
        }
    }
}
