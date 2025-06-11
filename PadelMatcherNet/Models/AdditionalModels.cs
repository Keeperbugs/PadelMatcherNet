using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PadelMatcherNet.Models
{
    // Collegamento tra Player e Tournament (molti-a-molti) - AGGIORNATO
    [Table("player_tournament")]
    public class PlayerTournament
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("player_id")]
        [Required]
        public string PlayerId { get; set; } = string.Empty;

        [Column("tournament_id")]
        [Required]
        public string TournamentId { get; set; } = string.Empty;

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigazione
        [JsonIgnore]
        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; } = null!;

        [JsonIgnore]
        [ForeignKey("TournamentId")]
        public virtual Tournament Tournament { get; set; } = null!;
    }

    // Statistiche per giocatore in un torneo specifico (AGGIORNATE)
    [Table("player_stats")]
    public class PlayerStats
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("player_id")]
        [Required]
        public string PlayerId { get; set; } = string.Empty;

        [Column("tournament_id")]
        [Required]
        public string TournamentId { get; set; } = string.Empty;

        [Column("points")]
        public int Points { get; set; } = 0;

        [Column("matches_played")]
        public int MatchesPlayed { get; set; } = 0;

        [Column("matches_won")]
        public int MatchesWon { get; set; } = 0;

        [Column("matches_drawn")]
        public int MatchesDrawn { get; set; } = 0;

        [Column("matches_lost")]
        public int MatchesLost { get; set; } = 0;

        [Column("sets_won")]
        public int SetsWon { get; set; } = 0;

        [Column("sets_lost")]
        public int SetsLost { get; set; } = 0;

        [Column("games_won")]
        public int GamesWon { get; set; } = 0;

        [Column("games_lost")]
        public int GamesLost { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigazione
        [JsonIgnore]
        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; } = null!;

        [JsonIgnore]
        [ForeignKey("TournamentId")]
        public virtual Tournament Tournament { get; set; } = null!;

        // Proprietà calcolate
        [NotMapped]
        public double WinRate => MatchesPlayed > 0 ? (double)MatchesWon / MatchesPlayed * 100 : 0;

        [NotMapped]
        public double DrawRate => MatchesPlayed > 0 ? (double)MatchesDrawn / MatchesPlayed * 100 : 0;

        [NotMapped]
        public double SetRatio => SetsLost > 0 ? (double)SetsWon / SetsLost : (SetsWon > 0 ? double.PositiveInfinity : 0);
    }

    // Impostazioni dell'applicazione aggiornate con punti pareggio
    [Table("tournament_settings")]
    public class AppSettings
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = "default_settings";

        [Column("darkMode")]
        public bool DarkMode { get; set; } = false;

        [Column("pairingStrategy")]
        public PairingStrategy PairingStrategy { get; set; } = PairingStrategy.BalancedAB;

        [Column("matchFormat")]
        public MatchFormat MatchFormat { get; set; } = MatchFormat.BestOfThree;

        [Column("pointsWin")]
        public int PointsWin { get; set; } = 3;

        [Column("pointsTieBreakLoss")]
        public int PointsTieBreakLoss { get; set; } = 1;

        [Column("pointsLoss")]
        public int PointsLoss { get; set; } = 0;

        [Column("pointsDraw")]
        public int PointsDraw { get; set; } = 1;

        [Column("allowDrawsInUnlimitedSet")]
        public bool AllowDrawsInUnlimitedSet { get; set; } = true;

        [Column("current_tournament_id")]
        public string? CurrentTournamentId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigazione
        [JsonIgnore]
        [ForeignKey("CurrentTournamentId")]
        public virtual Tournament? CurrentTournament { get; set; }
    }
}