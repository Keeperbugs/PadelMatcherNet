using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatcherNet.Migrations.TournamentDb
{
    /// <inheritdoc />
    public partial class UpdateForDrawSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allowDrawsInUnlimitedSet",
                table: "tournament_settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "pointsDraw",
                table: "tournament_settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "matches_drawn",
                table: "player_stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "matches_lost",
                table: "player_stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowDrawsInUnlimitedSet",
                table: "tournament_settings");

            migrationBuilder.DropColumn(
                name: "pointsDraw",
                table: "tournament_settings");

            migrationBuilder.DropColumn(
                name: "matches_drawn",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "matches_lost",
                table: "player_stats");
        }
    }
}
