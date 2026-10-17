using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resonance.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeparateBansAndSilences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StatusExpiresAt",
                table: "Users",
                newName: "SilenceExpiresAt");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Users",
                newName: "Ban");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SilencedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Ban" = CASE
                        WHEN "Ban" IN (2, 3) AND ("SilenceExpiresAt" IS NULL OR "SilenceExpiresAt" > CURRENT_TIMESTAMP)
                            THEN "Ban" - 1
                        ELSE 0
                    END,
                    "BannedAt" = CASE
                        WHEN "Ban" IN (2, 3) AND ("SilenceExpiresAt" IS NULL OR "SilenceExpiresAt" > CURRENT_TIMESTAMP)
                            THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END,
                    "SilencedAt" = CASE
                        WHEN "Ban" = 1 AND ("SilenceExpiresAt" IS NULL OR "SilenceExpiresAt" > CURRENT_TIMESTAMP)
                            THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END,
                    "SilenceExpiresAt" = CASE
                        WHEN "Ban" = 1 AND ("SilenceExpiresAt" IS NULL OR "SilenceExpiresAt" > CURRENT_TIMESTAMP)
                            THEN "SilenceExpiresAt"
                        ELSE NULL
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Ban" = CASE
                        WHEN "Ban" IN (1, 2) THEN "Ban" + 1
                        WHEN "SilencedAt" IS NOT NULL THEN 1
                        ELSE 0
                    END,
                    "SilenceExpiresAt" = CASE
                        WHEN "Ban" = 0 AND "SilencedAt" IS NOT NULL THEN "SilenceExpiresAt"
                        ELSE NULL
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "BannedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SilencedAt",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SilenceExpiresAt",
                table: "Users",
                newName: "StatusExpiresAt");

            migrationBuilder.RenameColumn(
                name: "Ban",
                table: "Users",
                newName: "Status");
        }
    }
}
