using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface IStatsService
    {
        Task CalculatePlayerStatsAsync(string tournamentId);
        Task CalculateAllPlayerStatsAsync();
        Task<List<PlayerStats>> GetTournamentPlayerStatsAsync(string tournamentId);
        Task<List<PlayerStats>> GetOverallPlayerStatsAsync();
        Task<PlayerStats?> GetPlayerTournamentStatsAsync(string playerId, string tournamentId);
        Task UpdatePlayerOverallStatsAsync(string playerId);
    }

    public class StatsService : IStatsService
    {
        private readonly TournamentDbContext _context;
        private readonly ISettingsService _settingsService;

        public StatsService(TournamentDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task CalculatePlayerStatsAsync(string tournamentId)
        {
            var settings = await _settingsService.GetSettingsAsync();

            // Ottieni tutte le partite completate del torneo (inclusi pareggi)
            var completedMatches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId &&
                           (m.Status == MatchStatus.Completed || m.Status == MatchStatus.Draw))
                .ToListAsync();

            // Ottieni tutti i giocatori del torneo
            var tournamentPlayers = await _context.PlayerTournaments
                .Where(pt => pt.TournamentId == tournamentId)
                .Include(pt => pt.Player)
                .Select(pt => pt.Player)
                .ToListAsync();

            // Inizializza le statistiche per tutti i giocatori
            var playerStatsDict = new Dictionary<string, PlayerStats>();
            foreach (var player in tournamentPlayers)
            {
                playerStatsDict[player.Id] = new PlayerStats
                {
                    Id = Guid.NewGuid().ToString(),
                    PlayerId = player.Id,
                    TournamentId = tournamentId,
                    MatchesPlayed = 0,
                    MatchesWon = 0,
                    MatchesDrawn = 0,
                    MatchesLost = 0,
                    SetsWon = 0,
                    SetsLost = 0,
                    GamesWon = 0,
                    GamesLost = 0,
                    Points = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Calcola le statistiche dalle partite completate
            foreach (var match in completedMatches)
            {
                var team1PlayerIds = new[] { match.Team1.Player1.Id, match.Team1.Player2.Id };
                var team2PlayerIds = new[] { match.Team2.Player1.Id, match.Team2.Player2.Id };
                var allPlayerIds = team1PlayerIds.Concat(team2PlayerIds);

                // Determina il risultato della partita
                bool isWin = match.Status == MatchStatus.Completed && !string.IsNullOrEmpty(match.WinnerTeamId);
                bool isDraw = match.Status == MatchStatus.Draw;

                int team1SetsWon = 0, team2SetsWon = 0;

                // Calcola set vinti per ogni team
                if (match.MatchFormat == MatchFormat.BestOfThree)
                {
                    foreach (var score in match.Scores)
                    {
                        if (int.TryParse(score.Team1Score?.ToString(), out int team1Score) &&
                            int.TryParse(score.Team2Score?.ToString(), out int team2Score))
                        {
                            if (team1Score > team2Score) team1SetsWon++;
                            else if (team2Score > team1Score) team2SetsWon++;
                        }
                    }
                }
                else if (match.MatchFormat == MatchFormat.UnlimitedSet)
                {
                    var score = match.Scores.FirstOrDefault();
                    if (score != null &&
                        int.TryParse(score.Team1Score?.ToString(), out int team1Score) &&
                        int.TryParse(score.Team2Score?.ToString(), out int team2Score))
                    {
                        if (team1Score > team2Score) team1SetsWon = 1;
                        else if (team2Score > team1Score) team2SetsWon = 1;
                        // Se team1Score == team2Score, rimane 0-0 (pareggio)
                    }
                }
                else if (match.MatchFormat == MatchFormat.GoldenPoint)
                {
                    // Per Golden Point, il vincitore prende 1 set simbolico
                    if (!isDraw && !string.IsNullOrEmpty(match.WinnerTeamId))
                    {
                        if (match.WinnerTeamId == match.Team1.Id) team1SetsWon = 1;
                        else team2SetsWon = 1;
                    }
                }

                // Aggiorna le statistiche per ogni giocatore
                foreach (var playerId in allPlayerIds)
                {
                    if (playerStatsDict.TryGetValue(playerId, out var stats))
                    {
                        var isTeam1Player = team1PlayerIds.Contains(playerId);

                        // Incrementa partite giocate
                        stats.MatchesPlayed++;

                        // Aggiorna set vinti/persi
                        if (isTeam1Player)
                        {
                            stats.SetsWon += team1SetsWon;
                            stats.SetsLost += team2SetsWon;
                        }
                        else
                        {
                            stats.SetsWon += team2SetsWon;
                            stats.SetsLost += team1SetsWon;
                        }

                        // Determina e assegna punti in base al risultato
                        if (isDraw)
                        {
                            // Pareggio
                            stats.MatchesDrawn++;
                            stats.Points += settings.PointsDraw;
                        }
                        else if (isWin)
                        {
                            var isWinner = match.WinnerTeamId == (isTeam1Player ? match.Team1.Id : match.Team2.Id);

                            if (isWinner)
                            {
                                // Vittoria
                                stats.MatchesWon++;
                                stats.Points += settings.PointsWin;
                            }
                            else
                            {
                                // Sconfitta
                                stats.MatchesLost++;

                                // Verifica se è una sconfitta al tie-break (solo per BestOfThree)
                                if (match.MatchFormat == MatchFormat.BestOfThree)
                                {
                                    int teamSetsWon = isTeam1Player ? team1SetsWon : team2SetsWon;
                                    int teamSetsLost = isTeam1Player ? team2SetsWon : team1SetsWon;

                                    if (teamSetsWon == 1 && teamSetsLost == 2)
                                    {
                                        stats.Points += settings.PointsTieBreakLoss;
                                    }
                                    else
                                    {
                                        stats.Points += settings.PointsLoss;
                                    }
                                }
                                else
                                {
                                    stats.Points += settings.PointsLoss;
                                }
                            }
                        }
                    }
                }
            }

            // Salva o aggiorna le statistiche nel database
            foreach (var playerStats in playerStatsDict.Values)
            {
                var existingStats = await _context.PlayerStats
                    .FirstOrDefaultAsync(ps => ps.PlayerId == playerStats.PlayerId && ps.TournamentId == tournamentId);

                if (existingStats == null)
                {
                    _context.PlayerStats.Add(playerStats);
                }
                else
                {
                    existingStats.MatchesPlayed = playerStats.MatchesPlayed;
                    existingStats.MatchesWon = playerStats.MatchesWon;
                    existingStats.MatchesDrawn = playerStats.MatchesDrawn;
                    existingStats.MatchesLost = playerStats.MatchesLost;
                    existingStats.SetsWon = playerStats.SetsWon;
                    existingStats.SetsLost = playerStats.SetsLost;
                    existingStats.GamesWon = playerStats.GamesWon;
                    existingStats.GamesLost = playerStats.GamesLost;
                    existingStats.Points = playerStats.Points;
                    existingStats.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // Aggiorna anche le statistiche generali dei giocatori
            foreach (var playerStatsPair in playerStatsDict)
            {
                await UpdatePlayerOverallStatsAsync(playerStatsPair.Key);
            }
        }

        public async Task CalculateAllPlayerStatsAsync()
        {
            var tournaments = await _context.Tournaments.ToListAsync();

            foreach (var tournament in tournaments)
            {
                await CalculatePlayerStatsAsync(tournament.Id);
            }
        }

        public async Task<List<PlayerStats>> GetTournamentPlayerStatsAsync(string tournamentId)
        {
            return await _context.PlayerStats
                .Include(ps => ps.Player)
                .Where(ps => ps.TournamentId == tournamentId)
                .OrderByDescending(ps => ps.Points)
                .ThenByDescending(ps => ps.MatchesWon)
                .ThenByDescending(ps => ps.SetsWon)
                .ThenBy(ps => ps.Player.Name)
                .ToListAsync();
        }

        public async Task<List<PlayerStats>> GetOverallPlayerStatsAsync()
        {
            // Calcola statistiche aggregate per tutti i tornei
            var allPlayerStats = await _context.PlayerStats
                .Include(ps => ps.Player)
                .GroupBy(ps => ps.PlayerId)
                .Select(g => new PlayerStats
                {
                    Id = g.Key,
                    PlayerId = g.Key,
                    TournamentId = "overall",
                    MatchesPlayed = g.Sum(ps => ps.MatchesPlayed),
                    MatchesWon = g.Sum(ps => ps.MatchesWon),
                    MatchesDrawn = g.Sum(ps => ps.MatchesDrawn),
                    MatchesLost = g.Sum(ps => ps.MatchesLost),
                    SetsWon = g.Sum(ps => ps.SetsWon),
                    SetsLost = g.Sum(ps => ps.SetsLost),
                    GamesWon = g.Sum(ps => ps.GamesWon),
                    GamesLost = g.Sum(ps => ps.GamesLost),
                    Points = g.Sum(ps => ps.Points),
                    Player = g.First().Player,
                    UpdatedAt = DateTime.UtcNow
                })
                .OrderByDescending(ps => ps.Points)
                .ThenByDescending(ps => ps.MatchesWon)
                .ThenByDescending(ps => ps.SetsWon)
                .ThenBy(ps => ps.Player.Name)
                .ToListAsync();

            return allPlayerStats;
        }

        public async Task<PlayerStats?> GetPlayerTournamentStatsAsync(string playerId, string tournamentId)
        {
            return await _context.PlayerStats
                .Include(ps => ps.Player)
                .Include(ps => ps.Tournament)
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId && ps.TournamentId == tournamentId);
        }

        public async Task UpdatePlayerOverallStatsAsync(string playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return;

            // Calcola le statistiche totali sommando tutte le statistiche dei tornei
            var allTournamentStats = await _context.PlayerStats
                .Where(ps => ps.PlayerId == playerId)
                .ToListAsync();

            player.MatchesPlayed = allTournamentStats.Sum(ps => ps.MatchesPlayed);
            player.MatchesWon = allTournamentStats.Sum(ps => ps.MatchesWon);
            player.SetsWon = allTournamentStats.Sum(ps => ps.SetsWon);
            player.SetsLost = allTournamentStats.Sum(ps => ps.SetsLost);
            player.GamesWon = allTournamentStats.Sum(ps => ps.GamesWon);
            player.GamesLost = allTournamentStats.Sum(ps => ps.GamesLost);
            player.Points = allTournamentStats.Sum(ps => ps.Points);
            player.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}