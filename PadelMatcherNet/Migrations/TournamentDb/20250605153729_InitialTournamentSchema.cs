using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatcherNet.Migrations.TournamentDb
{
    /// <inheritdoc />
    public partial class InitialTournamentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    surname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    contact = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    skillLevel = table.Column<string>(type: "TEXT", nullable: false),
                    matchesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    matchesWon = table.Column<int>(type: "INTEGER", nullable: false),
                    setsWon = table.Column<int>(type: "INTEGER", nullable: false),
                    setsLost = table.Column<int>(type: "INTEGER", nullable: false),
                    gamesWon = table.Column<int>(type: "INTEGER", nullable: false),
                    gamesLost = table.Column<int>(type: "INTEGER", nullable: false),
                    points = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    start_date = table.Column<string>(type: "TEXT", nullable: true),
                    end_date = table.Column<string>(type: "TEXT", nullable: true),
                    days = table.Column<int>(type: "INTEGER", nullable: false),
                    matches_per_day = table.Column<int>(type: "INTEGER", nullable: false),
                    max_players = table.Column<int>(type: "INTEGER", nullable: false),
                    current_round = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    player_ids = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    tournament_id = table.Column<string>(type: "TEXT", nullable: false),
                    round = table.Column<int>(type: "INTEGER", nullable: false),
                    team1 = table.Column<string>(type: "TEXT", nullable: false),
                    team2 = table.Column<string>(type: "TEXT", nullable: false),
                    scores = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    matchformat = table.Column<string>(type: "TEXT", nullable: false),
                    court = table.Column<string>(type: "TEXT", nullable: true),
                    winnerteamid = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_matches_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_stats",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    player_id = table.Column<string>(type: "TEXT", nullable: false),
                    tournament_id = table.Column<string>(type: "TEXT", nullable: false),
                    matches_played = table.Column<int>(type: "INTEGER", nullable: false),
                    matches_won = table.Column<int>(type: "INTEGER", nullable: false),
                    sets_won = table.Column<int>(type: "INTEGER", nullable: false),
                    sets_lost = table.Column<int>(type: "INTEGER", nullable: false),
                    games_won = table.Column<int>(type: "INTEGER", nullable: false),
                    games_lost = table.Column<int>(type: "INTEGER", nullable: false),
                    points = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_stats_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_stats_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_tournament",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    player_id = table.Column<string>(type: "TEXT", nullable: false),
                    tournament_id = table.Column<string>(type: "TEXT", nullable: false),
                    joined_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_tournament", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_tournament_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_tournament_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    darkMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    pairingStrategy = table.Column<string>(type: "TEXT", nullable: false),
                    matchFormat = table.Column<string>(type: "TEXT", nullable: false),
                    pointsWin = table.Column<int>(type: "INTEGER", nullable: false),
                    pointsTieBreakLoss = table.Column<int>(type: "INTEGER", nullable: false),
                    pointsLoss = table.Column<int>(type: "INTEGER", nullable: false),
                    current_tournament_id = table.Column<string>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_settings_tournaments_current_tournament_id",
                        column: x => x.current_tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matches_round",
                table: "matches",
                column: "round");

            migrationBuilder.CreateIndex(
                name: "IX_matches_status",
                table: "matches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_matches_tournament_id",
                table: "matches",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_player_id_tournament_id",
                table: "player_stats",
                columns: new[] { "player_id", "tournament_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_tournament_id",
                table: "player_stats",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_tournament_player_id_tournament_id",
                table: "player_tournament",
                columns: new[] { "player_id", "tournament_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_tournament_tournament_id",
                table: "player_tournament",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_players_name_surname",
                table: "players",
                columns: new[] { "name", "surname" });

            migrationBuilder.CreateIndex(
                name: "IX_players_nickname",
                table: "players",
                column: "nickname");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_settings_current_tournament_id",
                table: "tournament_settings",
                column: "current_tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_name",
                table: "tournaments",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_status",
                table: "tournaments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "player_stats");

            migrationBuilder.DropTable(
                name: "player_tournament");

            migrationBuilder.DropTable(
                name: "tournament_settings");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "tournaments");
        }
    }
}
