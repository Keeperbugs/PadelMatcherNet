using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface ITournamentService
    {
        Task<List<Tournament>> GetAllTournamentsAsync();
        Task<Tournament?> GetTournamentByIdAsync(string tournamentId);
        Task<Tournament> CreateTournamentAsync(Tournament tournament);
        Task<Tournament> UpdateTournamentAsync(Tournament tournament);
        Task<bool> DeleteTournamentAsync(string tournamentId);
        Task<bool> AddPlayerToTournamentAsync(string tournamentId, string playerId);
        Task<bool> RemovePlayerFromTournamentAsync(string tournamentId, string playerId);
        Task<List<Player>> GetTournamentPlayersAsync(string tournamentId);
        Task<List<Match>> GetTournamentMatchesAsync(string tournamentId);
        Task<List<PlayerStats>> GetTournamentStatsAsync(string tournamentId);
        Task<Tournament> SetCurrentTournamentAsync(string tournamentId);
        Task<Tournament?> GetCurrentTournamentAsync();
    }

    public class TournamentService : ITournamentService
    {
        private readonly TournamentDbContext _context;
        private readonly ISettingsService _settingsService;

        public TournamentService(TournamentDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return await _context.Tournaments
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Tournament?> GetTournamentByIdAsync(string tournamentId)
        {
            return await _context.Tournaments
                .Include(t => t.PlayerTournaments)
                    .ThenInclude(pt => pt.Player)
                .Include(t => t.Matches)
                .Include(t => t.PlayerStats)
                    .ThenInclude(ps => ps.Player)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
        }

        public async Task<Tournament> CreateTournamentAsync(Tournament tournament)
        {
            // Genera nuovo ID se non specificato
            if (string.IsNullOrEmpty(tournament.Id))
            {
                tournament.Id = Guid.NewGuid().ToString();
            }

            // Imposta i timestamp
            tournament.CreatedAt = DateTime.UtcNow;
            tournament.UpdatedAt = DateTime.UtcNow;

            // Valori di default
            tournament.CurrentRound = 1;
            
            // Assicurati che PlayerIds sia inizializzato
            if (tournament.PlayerIds == null)
            {
                tournament.PlayerIds = new List<string>();
            }

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            // Crea le relazioni PlayerTournament se ci sono giocatori
            if (tournament.PlayerIds.Any())
            {
                await AddPlayersToTournamentInternalAsync(tournament.Id, tournament.PlayerIds);
            }

            return tournament;
        }

        public async Task<Tournament> UpdateTournamentAsync(Tournament tournament)
        {
            var existingTournament = await _context.Tournaments
                .Include(t => t.PlayerTournaments)
                .FirstOrDefaultAsync(t => t.Id == tournament.Id);

            if (existingTournament == null)
            {
                throw new ArgumentException($"Tournament with ID {tournament.Id} not found");
            }

            // Aggiorna i campi
            existingTournament.Name = tournament.Name;
            existingTournament.Description = tournament.Description;
            existingTournament.StartDate = tournament.StartDate;
            existingTournament.EndDate = tournament.EndDate;
            existingTournament.Days = tournament.Days;
            existingTournament.MatchesPerDay = tournament.MatchesPerDay;
            existingTournament.MaxPlayers = tournament.MaxPlayers;
            existingTournament.Status = tournament.Status;
            existingTournament.CurrentRound = tournament.CurrentRound;
            existingTournament.UpdatedAt = DateTime.UtcNow;

            // Gestisci i cambiamenti nei giocatori
            var currentPlayerIds = existingTournament.PlayerIds;
            var newPlayerIds = tournament.PlayerIds ?? new List<string>();

            // Giocatori da rimuovere
            var playersToRemove = currentPlayerIds.Except(newPlayerIds).ToList();
            foreach (var playerId in playersToRemove)
            {
                await RemovePlayerFromTournamentInternalAsync(tournament.Id, playerId);
            }

            // Giocatori da aggiungere
            var playersToAdd = newPlayerIds.Except(currentPlayerIds).ToList();
            foreach (var playerId in playersToAdd)
            {
                await AddPlayerToTournamentInternalAsync(tournament.Id, playerId);
            }

            // Aggiorna la lista PlayerIds
            existingTournament.PlayerIds = newPlayerIds;

            await _context.SaveChangesAsync();
            return existingTournament;
        }

        public async Task<bool> DeleteTournamentAsync(string tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.PlayerTournaments)
                .Include(t => t.Matches)
                .Include(t => t.PlayerStats)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                return false;
            }

            // Rimuovi le relazioni PlayerTournament
            _context.PlayerTournaments.RemoveRange(tournament.PlayerTournaments);

            // Rimuovi le partite del torneo
            _context.Matches.RemoveRange(tournament.Matches);

            // Rimuovi le statistiche del torneo
            _context.PlayerStats.RemoveRange(tournament.PlayerStats);

            // Se questo era il torneo corrente, rimuovilo dalle impostazioni
            var settings = await _settingsService.GetSettingsAsync();
            if (settings.CurrentTournamentId == tournamentId)
            {
                await _settingsService.UpdateSettingAsync("CurrentTournamentId", (string?)null);
            }

            // Rimuovi il torneo
            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddPlayerToTournamentAsync(string tournamentId, string playerId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return false;
            }

            // Controlla se il torneo è già pieno
            if (tournament.CurrentPlayerCount >= tournament.MaxPlayers)
            {
                return false;
            }

            // Controlla se il giocatore è già nel torneo
            if (tournament.PlayerIds.Contains(playerId))
            {
                return false;
            }

            // Aggiungi il giocatore
            var updatedPlayerIds = tournament.PlayerIds.ToList();
            updatedPlayerIds.Add(playerId);
            tournament.PlayerIds = updatedPlayerIds;
            tournament.UpdatedAt = DateTime.UtcNow;

            await AddPlayerToTournamentInternalAsync(tournamentId, playerId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemovePlayerFromTournamentAsync(string tournamentId, string playerId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return false;
            }

            // Rimuovi il giocatore dalla lista
            var updatedPlayerIds = tournament.PlayerIds.Where(id => id != playerId).ToList();
            tournament.PlayerIds = updatedPlayerIds;
            tournament.UpdatedAt = DateTime.UtcNow;

            await RemovePlayerFromTournamentInternalAsync(tournamentId, playerId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Player>> GetTournamentPlayersAsync(string tournamentId)
        {
            return await _context.PlayerTournaments
                .Where(pt => pt.TournamentId == tournamentId)
                .Select(pt => pt.Player)
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Surname)
                .ToListAsync();
        }

        public async Task<List<Match>> GetTournamentMatchesAsync(string tournamentId)
        {
            return await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PlayerStats>> GetTournamentStatsAsync(string tournamentId)
        {
            return await _context.PlayerStats
                .Include(ps => ps.Player)
                .Where(ps => ps.TournamentId == tournamentId)
                .OrderByDescending(ps => ps.Points)
                .ThenByDescending(ps => ps.MatchesWon)
                .ThenBy(ps => ps.Player.Name)
                .ToListAsync();
        }

        public async Task<Tournament> SetCurrentTournamentAsync(string tournamentId)
        {
            var tournament = await GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                throw new ArgumentException($"Tournament with ID {tournamentId} not found");
            }

            await _settingsService.UpdateSettingAsync("CurrentTournamentId", tournamentId);
            return tournament;
        }

        public async Task<Tournament?> GetCurrentTournamentAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.CurrentTournamentId))
            {
                return null;
            }

            return await GetTournamentByIdAsync(settings.CurrentTournamentId);
        }

        private async Task AddPlayerToTournamentInternalAsync(string tournamentId, string playerId)
        {
            var existingRelation = await _context.PlayerTournaments
                .FirstOrDefaultAsync(pt => pt.TournamentId == tournamentId && pt.PlayerId == playerId);

            if (existingRelation == null)
            {
                var playerTournament = new PlayerTournament
                {
                    Id = Guid.NewGuid().ToString(),
                    PlayerId = playerId,
                    TournamentId = tournamentId,
                    JoinedAt = DateTime.UtcNow
                };

                _context.PlayerTournaments.Add(playerTournament);
            }
        }

        private async Task AddPlayersToTournamentInternalAsync(string tournamentId, List<string> playerIds)
        {
            foreach (var playerId in playerIds)
            {
                await AddPlayerToTournamentInternalAsync(tournamentId, playerId);
            }
        }

        private async Task RemovePlayerFromTournamentInternalAsync(string tournamentId, string playerId)
        {
            var playerTournament = await _context.PlayerTournaments
                .FirstOrDefaultAsync(pt => pt.TournamentId == tournamentId && pt.PlayerId == playerId);

            if (playerTournament != null)
            {
                _context.PlayerTournaments.Remove(playerTournament);
            }

            // Rimuovi anche le statistiche del giocatore per questo torneo
            var playerStats = await _context.PlayerStats
                .Where(ps => ps.TournamentId == tournamentId && ps.PlayerId == playerId)
                .ToListAsync();

            _context.PlayerStats.RemoveRange(playerStats);
        }
    }
}