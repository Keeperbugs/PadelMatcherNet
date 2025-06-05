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
            
            // Ottieni tutte le partite completate del torneo
            var completedMatches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId && m.Status == MatchStatus.Completed && !string.IsNullOrEmpty(m.WinnerTeamId))
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

                int team1SetsWon = 0, team2SetsWon = 0;
                int team1GamesWon = 0, team2GamesWon = 0;

                if (match.MatchFormat == MatchFormat.GoldenPoint)
                {
                    // Per il Golden Point, conta come un set vinto
                    if (match.WinnerTeamId == match.Team1.Id)
                    {
                        team1SetsWon = 1;
                        team2SetsWon = 0;
                    }
                    else
                    {
                        team1SetsWon = 0;
                        team2SetsWon = 1;
                    }
                }
                else
                {
                    // Per il Best of Three, conta i set e i game
                    foreach (var set in match.Scores)
                    {
                        if (int.TryParse(set.Team1Score?.ToString(), out int t1Score) && 
                            int.TryParse(set.Team2Score?.ToString(), out int t2Score))
                        {
                            if (t1Score > t2Score) team1SetsWon++;
                            else if (t2Score > t1Score) team2SetsWon++;

                            team1GamesWon += t1Score;
                            team2GamesWon += t2Score;
                        }
                    }
                }

                // Aggiorna le statistiche per ogni giocatore
                foreach (var playerId in allPlayerIds)
                {
                    if (!playerStatsDict.ContainsKey(playerId)) continue;

                    var stats = playerStatsDict[playerId];
                    stats.MatchesPlayed += 1;

                    bool isTeam1Player = team1PlayerIds.Contains(playerId);
                    bool isWinner = isTeam1Player ? match.WinnerTeamId == match.Team1.Id : match.WinnerTeamId == match.Team2.Id;

                    if (isWinner)
                    {
                        stats.MatchesWon += 1;
                        stats.Points += settings.PointsWin;
                    }
                    else
                    {
                        // Determina se è stata una sconfitta al tie-break
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
                            // Aggiorna i set vinti e persi
                            if (isTeam1Player)
                            {
                                stats.SetsWon += team1SetsWon;
                                stats.SetsLost += team2SetsWon;
                                stats.GamesWon += team1GamesWon;
                                stats.GamesLost += team2GamesWon;
                            }
                            else
                            {
                                stats.SetsWon += team2SetsWon;
                                stats.SetsLost += team1SetsWon;
                                stats.GamesWon += team2GamesWon;
                                stats.GamesLost += team1GamesWon;
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
            return await _context.Players
                .OrderByDescending(p => p.Points)
                .ThenByDescending(p => p.MatchesWon)
                .ThenByDescending(p => p.SetsWon)
                .ThenBy(p => p.Name)
                .Select(p => new PlayerStats
                {
                    Id = p.Id,
                    PlayerId = p.Id,
                    TournamentId = "overall",
                    MatchesPlayed = p.MatchesPlayed,
                    MatchesWon = p.MatchesWon,
                    SetsWon = p.SetsWon,
                    SetsLost = p.SetsLost,
                    GamesWon = p.GamesWon,
                    GamesLost = p.GamesLost,
                    Points = p.Points,
                    Player = p,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
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