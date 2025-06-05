using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelMatcherNet.Models
{
    // Tabella di collegamento many-to-many tra Player e Tournament
    [Table("player_tournament")]
    public class PlayerTournament
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("player_id")]
        public string PlayerId { get; set; } = string.Empty;

        [Required]
        [Column("tournament_id")]
        public string TournamentId { get; set; } = string.Empty;

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }

        // Navigazione
        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; } = null!;

        [ForeignKey("TournamentId")]
        public virtual Tournament Tournament { get; set; } = null!;
    }

    // Statistiche specifiche per torneo
    [Table("player_stats")]
    public class PlayerStats
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("player_id")]
        public string PlayerId { get; set; } = string.Empty;

        [Required]
        [Column("tournament_id")]
        public string TournamentId { get; set; } = string.Empty;

        [Column("matches_played")]
        public int MatchesPlayed { get; set; } = 0;

        [Column("matches_won")]
        public int MatchesWon { get; set; } = 0;

        [Column("sets_won")]
        public int SetsWon { get; set; } = 0;

        [Column("sets_lost")]
        public int SetsLost { get; set; } = 0;

        [Column("games_won")]
        public int GamesWon { get; set; } = 0;

        [Column("games_lost")]
        public int GamesLost { get; set; } = 0;

        [Column("points")]
        public int Points { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigazione
        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; } = null!;

        [ForeignKey("TournamentId")]
        public virtual Tournament Tournament { get; set; } = null!;

        // Proprietà calcolate
        [NotMapped]
        public double WinRate => MatchesPlayed > 0 ? (double)MatchesWon / MatchesPlayed * 100 : 0;

        [NotMapped]
        public double SetRatio => SetsLost > 0 ? (double)SetsWon / SetsLost : (SetsWon > 0 ? double.PositiveInfinity : 0);
    }

    // Impostazioni dell'applicazione
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

        [Column("current_tournament_id")]
        public string? CurrentTournamentId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigazione
        [ForeignKey("CurrentTournamentId")]
        public virtual Tournament? CurrentTournament { get; set; }
    }
}