using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface ICleanupService
    {
        Task CleanupTestDataAsync();
        Task CleanupAllDataAsync();
        Task<CleanupResult> GetDataCountsAsync();
    }

    public class CleanupService : ICleanupService
    {
        private readonly TournamentDbContext _context;
        private readonly ISettingsService _settingsService;

        public CleanupService(TournamentDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task CleanupTestDataAsync()
        {
            // Rimuovi tornei di test (che contengono "Test" nel nome)
            var testTournaments = await _context.Tournaments
                .Where(t => t.Name.Contains("Test"))
                .ToListAsync();

            foreach (var tournament in testTournaments)
            {
                // Rimuovi partite del torneo
                var matches = await _context.Matches
                    .Where(m => m.TournamentId == tournament.Id)
                    .ToListAsync();
                _context.Matches.RemoveRange(matches);

                // Rimuovi statistiche del torneo
                var stats = await _context.PlayerStats
                    .Where(ps => ps.TournamentId == tournament.Id)
                    .ToListAsync();
                _context.PlayerStats.RemoveRange(stats);

                // Rimuovi relazioni player-tournament
                var playerTournaments = await _context.PlayerTournaments
                    .Where(pt => pt.TournamentId == tournament.Id)
                    .ToListAsync();
                _context.PlayerTournaments.RemoveRange(playerTournaments);
            }

            // Rimuovi i tornei di test
            _context.Tournaments.RemoveRange(testTournaments);

            // Rimuovi giocatori di test (che iniziano con "Test")
            var testPlayers = await _context.Players
                .Where(p => p.Name.StartsWith("Test"))
                .ToListAsync();

            foreach (var player in testPlayers)
            {
                // Rimuovi tutte le statistiche del giocatore
                var playerStats = await _context.PlayerStats
                    .Where(ps => ps.PlayerId == player.Id)
                    .ToListAsync();
                _context.PlayerStats.RemoveRange(playerStats);

                // Rimuovi tutte le relazioni player-tournament
                var playerTournaments = await _context.PlayerTournaments
                    .Where(pt => pt.PlayerId == player.Id)
                    .ToListAsync();
                _context.PlayerTournaments.RemoveRange(playerTournaments);

                // Aggiorna i tornei rimuovendo il giocatore dai PlayerIds JSON
                var tournaments = await _context.Tournaments.ToListAsync();
                foreach (var tournament in tournaments)
                {
                    if (tournament.PlayerIds.Contains(player.Id))
                    {
                        var updatedPlayerIds = tournament.PlayerIds.Where(id => id != player.Id).ToList();
                        tournament.PlayerIds = updatedPlayerIds;
                        tournament.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // Rimuovi i giocatori di test
            _context.Players.RemoveRange(testPlayers);

            // Reset del torneo corrente se era un torneo di test
            var settings = await _settingsService.GetSettingsAsync();
            if (!string.IsNullOrEmpty(settings.CurrentTournamentId))
            {
                var currentTournamentExists = await _context.Tournaments
                    .AnyAsync(t => t.Id == settings.CurrentTournamentId);
                
                if (!currentTournamentExists)
                {
                    await _settingsService.UpdateSettingAsync("CurrentTournamentId", (string?)null);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task CleanupAllDataAsync()
        {
            // ATTENZIONE: Questa operazione rimuove TUTTI i dati!
            
            // Rimuovi tutte le partite
            var allMatches = await _context.Matches.ToListAsync();
            _context.Matches.RemoveRange(allMatches);

            // Rimuovi tutte le statistiche
            var allStats = await _context.PlayerStats.ToListAsync();
            _context.PlayerStats.RemoveRange(allStats);

            // Rimuovi tutte le relazioni player-tournament
            var allPlayerTournaments = await _context.PlayerTournaments.ToListAsync();
            _context.PlayerTournaments.RemoveRange(allPlayerTournaments);

            // Rimuovi tutti i tornei
            var allTournaments = await _context.Tournaments.ToListAsync();
            _context.Tournaments.RemoveRange(allTournaments);

            // Reset statistiche di tutti i giocatori
            var allPlayers = await _context.Players.ToListAsync();
            foreach (var player in allPlayers)
            {
                player.MatchesPlayed = 0;
                player.MatchesWon = 0;
                player.SetsWon = 0;
                player.SetsLost = 0;
                player.GamesWon = 0;
                player.GamesLost = 0;
                player.Points = 0;
                player.UpdatedAt = DateTime.UtcNow;
            }

            // Reset impostazioni
            await _settingsService.UpdateSettingAsync("CurrentTournamentId", (string?)null);

            await _context.SaveChangesAsync();
        }

        public async Task<CleanupResult> GetDataCountsAsync()
        {
            var result = new CleanupResult
            {
                TotalPlayers = await _context.Players.CountAsync(),
                TestPlayers = await _context.Players.CountAsync(p => p.Name.StartsWith("Test")),
                TotalTournaments = await _context.Tournaments.CountAsync(),
                TestTournaments = await _context.Tournaments.CountAsync(t => t.Name.Contains("Test")),
                TotalMatches = await _context.Matches.CountAsync(),
                TestMatches = await _context.Matches
                    .Join(_context.Tournaments, m => m.TournamentId, t => t.Id, (m, t) => t)
                    .CountAsync(t => t.Name.Contains("Test")),
                TotalStats = await _context.PlayerStats.CountAsync()
            };

            return result;
        }
    }

    public class CleanupResult
    {
        public int TotalPlayers { get; set; }
        public int TestPlayers { get; set; }
        public int TotalTournaments { get; set; }
        public int TestTournaments { get; set; }
        public int TotalMatches { get; set; }
        public int TestMatches { get; set; }
        public int TotalStats { get; set; }
    }
}