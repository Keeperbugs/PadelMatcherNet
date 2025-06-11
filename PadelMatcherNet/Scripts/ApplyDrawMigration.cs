using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using System.Data;

namespace PadelMatcherNet.Scripts
{
    /// <summary>
    /// Script per applicare le migrazioni del supporto pareggi
    /// Eseguire questo script dopo aver aggiornato i modelli
    /// </summary>
    public static class ApplyDrawMigration
    {
        public static async Task ExecuteAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();

            try
            {
                Console.WriteLine("🔄 Applicazione migrazioni per supporto pareggi...");

                // Applica le migrazioni pending
                await context.Database.MigrateAsync();

                // Verifica che le nuove colonne esistano
                var hasPointsDraw = await HasColumnAsync(context, "tournament_settings", "pointsDraw");
                var hasAllowDraws = await HasColumnAsync(context, "tournament_settings", "allowDrawsInUnlimitedSet");
                var hasMatchesDrawn = await HasColumnAsync(context, "player_stats", "matches_drawn");

                if (hasPointsDraw && hasAllowDraws && hasMatchesDrawn)
                {
                    Console.WriteLine("✅ Tutte le nuove colonne sono state create con successo!");

                    // Aggiorna le impostazioni predefinite se non già fatto
                    await UpdateDefaultSettings(context);

                    Console.WriteLine("✅ Migrazione completata con successo!");
                    Console.WriteLine("🎯 Funzionalità disponibili:");
                    Console.WriteLine("   - MatchStatus.Draw per pareggi");
                    Console.WriteLine("   - MatchFormat.UnlimitedSet per set illimitati");
                    Console.WriteLine("   - Punti pareggio configurabili");
                    Console.WriteLine("   - Statistiche pareggi per giocatori");
                }
                else
                {
                    Console.WriteLine("⚠️ Alcune colonne potrebbero non essere state create correttamente");
                    Console.WriteLine($"   pointsDraw: {hasPointsDraw}");
                    Console.WriteLine($"   allowDrawsInUnlimitedSet: {hasAllowDraws}");
                    Console.WriteLine($"   matches_drawn: {hasMatchesDrawn}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore durante la migrazione: {ex.Message}");
                throw;
            }
        }

        private static async Task<bool> HasColumnAsync(TournamentDbContext context, string tableName, string columnName)
        {
            try
            {
                var sql = $"PRAGMA table_info({tableName})";
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString("name");
                    if (name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static async Task UpdateDefaultSettings(TournamentDbContext context)
        {
            var settings = await context.AppSettings
                .FirstOrDefaultAsync(s => s.Id == "default_settings");

            if (settings != null)
            {
                // Assicurati che i nuovi campi abbiano valori predefiniti
                if (settings.PointsDraw == 0)
                {
                    settings.PointsDraw = 1;
                }

                // AllowDrawsInUnlimitedSet dovrebbe essere già true per default
                // ma assicuriamoci
                if (!settings.AllowDrawsInUnlimitedSet)
                {
                    settings.AllowDrawsInUnlimitedSet = true;
                }

                settings.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                Console.WriteLine("✅ Impostazioni predefinite aggiornate:");
                Console.WriteLine($"   Punti pareggio: {settings.PointsDraw}");
                Console.WriteLine($"   Pareggi in set illimitato: {settings.AllowDrawsInUnlimitedSet}");
            }
        }

        /// <summary>
        /// Metodo di utilità per eseguire la migrazione da Program.cs
        /// </summary>
        public static async Task RunMigrationIfNeeded(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();

            // Verifica se ci sono migrazioni pending
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"🔄 Trovate {pendingMigrations.Count()} migrazioni pending...");
                await ExecuteAsync(app.Services);
            }
            else
            {
                Console.WriteLine("✅ Database aggiornato, nessuna migrazione necessaria");
            }
        }
    }
}