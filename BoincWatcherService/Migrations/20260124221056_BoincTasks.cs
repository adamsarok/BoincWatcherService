using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class BoincTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoincTasks",
                columns: table => new
                {
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    TaskName = table.Column<string>(type: "text", nullable: false),
                    HostName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoincTasks", x => new { x.ProjectName, x.TaskName, x.HostName });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoincTasks");
        }
    }
}
