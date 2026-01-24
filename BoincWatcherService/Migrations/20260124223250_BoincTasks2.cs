using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class BoincTasks2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "BoincTasks",
                newName: "ReceivedTime");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CurrentCpuTime",
                table: "BoincTasks",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ElapsedTime",
                table: "BoincTasks",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedCpuTimeRemaining",
                table: "BoincTasks",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<double>(
                name: "FractionDone",
                table: "BoincTasks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCpuTime",
                table: "BoincTasks");

            migrationBuilder.DropColumn(
                name: "ElapsedTime",
                table: "BoincTasks");

            migrationBuilder.DropColumn(
                name: "EstimatedCpuTimeRemaining",
                table: "BoincTasks");

            migrationBuilder.DropColumn(
                name: "FractionDone",
                table: "BoincTasks");

            migrationBuilder.RenameColumn(
                name: "ReceivedTime",
                table: "BoincTasks",
                newName: "CreatedAt");
        }
    }
}
