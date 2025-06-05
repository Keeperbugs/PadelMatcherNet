using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelMatcherNet.Models
{
    // Classe per rappresentare un set di punteggi
    public class MatchSetScore
    {
        public int SetNumber { get; set; }
        public object Team1Score { get; set; } = "";  // Può essere int o "GP" per Golden Point
        public object Team2Score { get; set; } = "";  // Può essere int o "GP" per Golden Point
    }

    // Classe per rappresentare una squadra
    public class Team
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Player Player1 { get; set; } = null!;
        public Player Player2 { get; set; } = null!;

        [NotMapped]
        public string DisplayName => $"{Player1.DisplayName} / {Player2.DisplayName}";
    }

    [Table("matches")]
    public class Match
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("tournament_id")]
        public string TournamentId { get; set; } = string.Empty;

        [Column("round")]
        public int Round { get; set; }

        [Column("team1", TypeName = "TEXT")]
        public string Team1Json { get; set; } = "{}";

        [Column("team2", TypeName = "TEXT")]
        public string Team2Json { get; set; } = "{}";

        [Column("scores", TypeName = "TEXT")]
        public string ScoresJson { get; set; } = "[]";

        [Column("status")]
        public MatchStatus Status { get; set; } = MatchStatus.Pending;

        [Column("matchformat")]
        public MatchFormat MatchFormat { get; set; } = MatchFormat.BestOfThree;

        [Column("court")]
        public string? Court { get; set; }

        [Column("winnerteamid")]
        public string? WinnerTeamId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Proprietà per gestire Team1 come oggetto
        [NotMapped]
        public Team Team1
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(Team1Json) 
                        ? new Team() 
                        : System.Text.Json.JsonSerializer.Deserialize<Team>(Team1Json) ?? new Team();
                }
                catch
                {
                    return new Team();
                }
            }
            set
            {
                Team1Json = System.Text.Json.JsonSerializer.Serialize(value ?? new Team());
            }
        }

        // Proprietà per gestire Team2 come oggetto
        [NotMapped]
        public Team Team2
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(Team2Json) 
                        ? new Team() 
                        : System.Text.Json.JsonSerializer.Deserialize<Team>(Team2Json) ?? new Team();
                }
                catch
                {
                    return new Team();
                }
            }
            set
            {
                Team2Json = System.Text.Json.JsonSerializer.Serialize(value ?? new Team());
            }
        }

        // Proprietà per gestire Scores come lista
        [NotMapped]
        public List<MatchSetScore> Scores
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(ScoresJson) 
                        ? new List<MatchSetScore>() 
                        : System.Text.Json.JsonSerializer.Deserialize<List<MatchSetScore>>(ScoresJson) ?? new List<MatchSetScore>();
                }
                catch
                {
                    return new List<MatchSetScore>();
                }
            }
            set
            {
                ScoresJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<MatchSetScore>());
            }
        }

        // Navigazione
        [ForeignKey("TournamentId")]
        public virtual Tournament Tournament { get; set; } = null!;

        // Proprietà calcolate
        [NotMapped]
        public string ScoreDisplay
        {
            get
            {
                if (!Scores.Any()) return "0-0";
                
                return string.Join(" ", Scores.Select(s => 
                {
                    if (s.Team1Score.ToString() == "GP" || s.Team2Score.ToString() == "GP")
                        return s.Team1Score.ToString() == "GP" ? "GP vinto" : "GP perso";
                    return $"{s.Team1Score}-{s.Team2Score}";
                }));
            }
        }
    }
}