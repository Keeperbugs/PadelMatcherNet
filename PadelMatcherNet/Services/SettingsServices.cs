using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface ISettingsService
    {
        Task<AppSettings> GetSettingsAsync();
        Task<AppSettings> UpdateSettingsAsync(AppSettings settings);
        Task<AppSettings> UpdateSettingAsync<T>(string propertyName, T value);
    }

    public class SettingsService : ISettingsService
    {
        private readonly TournamentDbContext _context;
        private const string DEFAULT_SETTINGS_ID = "default_settings";

        public SettingsService(TournamentDbContext context)
        {
            _context = context;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Include(s => s.CurrentTournament)
                .FirstOrDefaultAsync(s => s.Id == DEFAULT_SETTINGS_ID);

            if (settings == null)
            {
                // Crea le impostazioni predefinite se non esistono
                settings = new AppSettings
                {
                    Id = DEFAULT_SETTINGS_ID,
                    DarkMode = false,
                    PairingStrategy = PairingStrategy.BalancedAB,
                    MatchFormat = MatchFormat.BestOfThree,
                    PointsWin = 3,
                    PointsTieBreakLoss = 1,
                    PointsLoss = 0,
                    PointsDraw = 1,
                    AllowDrawsInUnlimitedSet = true,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AppSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<AppSettings> UpdateSettingsAsync(AppSettings settings)
        {
            var existingSettings = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Id == DEFAULT_SETTINGS_ID);

            if (existingSettings == null)
            {
                settings.Id = DEFAULT_SETTINGS_ID;
                settings.UpdatedAt = DateTime.UtcNow;
                _context.AppSettings.Add(settings);
            }
            else
            {
                // Aggiorna solo i campi necessari
                existingSettings.DarkMode = settings.DarkMode;
                existingSettings.PairingStrategy = settings.PairingStrategy;
                existingSettings.MatchFormat = settings.MatchFormat;
                existingSettings.PointsWin = settings.PointsWin;
                existingSettings.PointsTieBreakLoss = settings.PointsTieBreakLoss;
                existingSettings.PointsLoss = settings.PointsLoss;
                existingSettings.PointsDraw = settings.PointsDraw;
                existingSettings.AllowDrawsInUnlimitedSet = settings.AllowDrawsInUnlimitedSet;
                existingSettings.CurrentTournamentId = settings.CurrentTournamentId;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Ricarica le impostazioni con il torneo corrente
            return await GetSettingsAsync();
        }

        public async Task<AppSettings> UpdateSettingAsync<T>(string propertyName, T value)
        {
            var settings = await GetSettingsAsync();

            var property = typeof(AppSettings).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(settings, value);
                return await UpdateSettingsAsync(settings);
            }

            throw new ArgumentException($"Property '{propertyName}' not found or not writable on AppSettings");
        }
    }
}