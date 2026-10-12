using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resonance.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPrivilegesAndBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Badges",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperuser",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Privileges",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badges",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSuperuser",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Privileges",
                table: "Users");
        }
    }
}
