using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelMatcherNet.Models
{
    [Table("players")]
    public class Player
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("surname")]
        public string Surname { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("nickname")]
        public string? Nickname { get; set; }

        [MaxLength(200)]
        [Column("contact")]
        public string? Contact { get; set; }

        [Column("skillLevel")]
        public SkillLevel SkillLevel { get; set; } = SkillLevel.Unassigned;

        [Column("matchesPlayed")]
        public int MatchesPlayed { get; set; } = 0;

        [Column("matchesWon")]
        public int MatchesWon { get; set; } = 0;

        [Column("setsWon")]
        public int SetsWon { get; set; } = 0;

        [Column("setsLost")]
        public int SetsLost { get; set; } = 0;

        [Column("gamesWon")]
        public int GamesWon { get; set; } = 0;

        [Column("gamesLost")]
        public int GamesLost { get; set; } = 0;

        [Column("points")]
        public int Points { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Proprietà calcolate
        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(Nickname) ? Nickname : $"{Name} {Surname}";

        [NotMapped]
        public string FullName => $"{Name} {Surname}";

        [NotMapped]
        public double WinRate => MatchesPlayed > 0 ? (double)MatchesWon / MatchesPlayed * 100 : 0;

        [NotMapped]
        public double SetRatio => SetsLost > 0 ? (double)SetsWon / SetsLost : (SetsWon > 0 ? double.PositiveInfinity : 0);

        // Navigazione
        public virtual ICollection<PlayerTournament> PlayerTournaments { get; set; } = new List<PlayerTournament>();
        public virtual ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>();
    }
}