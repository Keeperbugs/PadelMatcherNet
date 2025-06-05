using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Models;
using System.Text.Json;

namespace PadelMatcherNet.Data
{
    public class TournamentDbContext : DbContext
    {
        public TournamentDbContext(DbContextOptions<TournamentDbContext> options) : base(options)
        {
        }

        // DbSets per le entità principali
        public DbSet<Player> Players { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<PlayerTournament> PlayerTournaments { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione Player
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                
                // Valori di default per i timestamp
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // Conversione enum SkillLevel per il database
                entity.Property(e => e.SkillLevel)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<SkillLevel>(v)
                    );

                entity.HasIndex(e => new { e.Name, e.Surname });
                entity.HasIndex(e => e.Nickname);
            });

            // Configurazione Tournament
            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Valori di default per i timestamp
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

                // Conversione enum TournamentStatus
                entity.Property(e => e.Status)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<TournamentStatus>(v)
                    );

                // Conversione DateOnly per SQLite
                entity.Property(e => e.StartDate)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToString("yyyy-MM-dd") : null,
                        v => !string.IsNullOrEmpty(v) ? DateOnly.Parse(v) : null
                    );

                entity.Property(e => e.EndDate)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToString("yyyy-MM-dd") : null,
                        v => !string.IsNullOrEmpty(v) ? DateOnly.Parse(v) : null
                    );

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
            });

            // Configurazione Match
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Conversione enum MatchStatus
                entity.Property(e => e.Status)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<MatchStatus>(v)
                    );

                // Conversione enum MatchFormat
                entity.Property(e => e.MatchFormat)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<MatchFormat>(v)
                    );

                // Relazione con Tournament
                entity.HasOne(e => e.Tournament)
                    .WithMany(t => t.Matches)
                    .HasForeignKey(e => e.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TournamentId);
                entity.HasIndex(e => e.Round);
                entity.HasIndex(e => e.Status);
            });

            // Configurazione PlayerTournament (tabella di collegamento)
            modelBuilder.Entity<PlayerTournament>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Chiave composta alternativa per evitare duplicati
                entity.HasIndex(e => new { e.PlayerId, e.TournamentId }).IsUnique();

                // Relazioni
                entity.HasOne(pt => pt.Player)
                    .WithMany(p => p.PlayerTournaments)
                    .HasForeignKey(pt => pt.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.Tournament)
                    .WithMany(t => t.PlayerTournaments)
                    .HasForeignKey(pt => pt.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurazione PlayerStats
            modelBuilder.Entity<PlayerStats>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Chiave composta per player + tournament
                entity.HasIndex(e => new { e.PlayerId, e.TournamentId }).IsUnique();

                // Relazioni
                entity.HasOne(ps => ps.Player)
                    .WithMany(p => p.PlayerStats)
                    .HasForeignKey(ps => ps.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ps => ps.Tournament)
                    .WithMany(t => t.PlayerStats)
                    .HasForeignKey(ps => ps.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurazione AppSettings
            modelBuilder.Entity<AppSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Conversione enum PairingStrategy
                entity.Property(e => e.PairingStrategy)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<PairingStrategy>(v)
                    );

                // Conversione enum MatchFormat
                entity.Property(e => e.MatchFormat)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<MatchFormat>(v)
                    );

                // Relazione opzionale con Tournament corrente
                entity.HasOne(s => s.CurrentTournament)
                    .WithMany()
                    .HasForeignKey(s => s.CurrentTournamentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Rimuoviamo il seed data per evitare problemi con valori dinamici
            // Le impostazioni predefinite verranno create programmaticamente se necessario
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Aggiorna automaticamente UpdatedAt per le entità modificate
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}