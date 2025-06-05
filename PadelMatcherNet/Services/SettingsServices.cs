using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface IPlayerService
    {
        Task<List<Player>> GetAllPlayersAsync();
        Task<Player?> GetPlayerByIdAsync(string playerId);
        Task<Player> AddPlayerAsync(Player player);
        Task<Player> UpdatePlayerAsync(Player player);
        Task<bool> DeletePlayerAsync(string playerId);
        Task<List<Player>> SearchPlayersAsync(string searchTerm, SkillLevel? skillLevel = null);
        Task<List<Tournament>> GetPlayerTournamentsAsync(string playerId);
        Task<List<PlayerStats>> GetPlayerStatsAsync(string playerId);
    }

    public class PlayerService : IPlayerService
    {
        private readonly TournamentDbContext _context;

        public PlayerService(TournamentDbContext context)
        {
            _context = context;
        }

        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _context.Players
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Surname)
                .ToListAsync();
        }

        public async Task<Player?> GetPlayerByIdAsync(string playerId)
        {
            return await _context.Players
                .Include(p => p.PlayerTournaments)
                    .ThenInclude(pt => pt.Tournament)
                .Include(p => p.PlayerStats)
                    .ThenInclude(ps => ps.Tournament)
                .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        public async Task<Player> AddPlayerAsync(Player player)
        {
            // Genera nuovo ID se non specificato
            if (string.IsNullOrEmpty(player.Id))
            {
                player.Id = Guid.NewGuid().ToString();
            }

            // Imposta i timestamp
            player.CreatedAt = DateTime.UtcNow;
            player.UpdatedAt = DateTime.UtcNow;

            // Inizializza le statistiche
            player.MatchesPlayed = 0;
            player.MatchesWon = 0;
            player.SetsWon = 0;
            player.SetsLost = 0;
            player.GamesWon = 0;
            player.GamesLost = 0;
            player.Points = 0;

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return player;
        }

        public async Task<Player> UpdatePlayerAsync(Player player)
        {
            var existingPlayer = await _context.Players.FindAsync(player.Id);
            if (existingPlayer == null)
            {
                throw new ArgumentException($"Player with ID {player.Id} not found");
            }

            // Aggiorna solo i campi modificabili
            existingPlayer.Name = player.Name;
            existingPlayer.Surname = player.Surname;
            existingPlayer.Nickname = player.Nickname;
            existingPlayer.Contact = player.Contact;
            existingPlayer.SkillLevel = player.SkillLevel;
            existingPlayer.UpdatedAt = DateTime.UtcNow;

            // Mantieni le statistiche esistenti (non le sovrascriviamo)
            if (player.MatchesPlayed > 0 || player.Points > 0)
            {
                existingPlayer.MatchesPlayed = player.MatchesPlayed;
                existingPlayer.MatchesWon = player.MatchesWon;
                existingPlayer.SetsWon = player.SetsWon;
                existingPlayer.SetsLost = player.SetsLost;
                existingPlayer.GamesWon = player.GamesWon;
                existingPlayer.GamesLost = player.GamesLost;
                existingPlayer.Points = player.Points;
            }

            await _context.SaveChangesAsync();
            return existingPlayer;
        }

        public async Task<bool> DeletePlayerAsync(string playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return false;
            }

            // Rimuovi il giocatore da tutti i tornei
            var playerTournaments = await _context.PlayerTournaments
                .Where(pt => pt.PlayerId == playerId)
                .ToListAsync();
            _context.PlayerTournaments.RemoveRange(playerTournaments);

            // Rimuovi tutte le statistiche del giocatore
            var playerStats = await _context.PlayerStats
                .Where(ps => ps.PlayerId == playerId)
                .ToListAsync();
            _context.PlayerStats.RemoveRange(playerStats);

            // Aggiorna i tornei rimuovendo il giocatore dai PlayerIds JSON
            var tournaments = await _context.Tournaments.ToListAsync();
            foreach (var tournament in tournaments)
            {
                if (tournament.PlayerIds.Contains(playerId))
                {
                    var updatedPlayerIds = tournament.PlayerIds.Where(id => id != playerId).ToList();
                    tournament.PlayerIds = updatedPlayerIds;
                }
            }

            // Rimuovi il giocatore
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Player>> SearchPlayersAsync(string searchTerm, SkillLevel? skillLevel = null)
        {
            var query = _context.Players.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(lowerSearchTerm) ||
                    p.Surname.ToLower().Contains(lowerSearchTerm) ||
                    (p.Nickname != null && p.Nickname.ToLower().Contains(lowerSearchTerm))
                );
            }

            if (skillLevel.HasValue)
            {
                query = query.Where(p => p.SkillLevel == skillLevel.Value);
            }

            return await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Surname)
                .ToListAsync();
        }

        public async Task<List<Tournament>> GetPlayerTournamentsAsync(string playerId)
        {
            return await _context.PlayerTournaments
                .Where(pt => pt.PlayerId == playerId)
                .Select(pt => pt.Tournament)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PlayerStats>> GetPlayerStatsAsync(string playerId)
        {
            return await _context.PlayerStats
                .Include(ps => ps.Tournament)
                .Where(ps => ps.PlayerId == playerId)
                .OrderByDescending(ps => ps.Tournament.CreatedAt)
                .ToListAsync();
        }
    }
}