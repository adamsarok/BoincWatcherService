using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoincWatcherService.Migrations
{
    /// <inheritdoc />
    public partial class BoincApp2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserFriendlyName",
                table: "BoincApps",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserFriendlyName",
                table: "BoincApps");
        }
    }
}
