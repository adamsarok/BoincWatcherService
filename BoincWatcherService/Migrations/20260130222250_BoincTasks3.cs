using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class BoincTasks3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "FinalCpuTime",
                table: "BoincTasks",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FinalElapsedTime",
                table: "BoincTasks",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalCpuTime",
                table: "BoincTasks");

            migrationBuilder.DropColumn(
                name: "FinalElapsedTime",
                table: "BoincTasks");
        }
    }
}
