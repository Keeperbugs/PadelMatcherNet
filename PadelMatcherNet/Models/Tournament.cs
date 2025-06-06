using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PadelMatcherNet.Models
{
    [Table("tournaments")]
    public class Tournament
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("start_date")]
        public DateOnly? StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [Column("days")]
        public int Days { get; set; } = 12;

        [Column("matches_per_day")]
        public int MatchesPerDay { get; set; } = 6;

        [Column("max_players")]
        public int MaxPlayers { get; set; } = 24;

        [Column("current_round")]
        public int CurrentRound { get; set; } = 1;

        [Column("status")]
        public TournamentStatus Status { get; set; } = TournamentStatus.Draft;

        [Column("player_ids", TypeName = "TEXT")]
        public string PlayerIdsJson { get; set; } = "[]";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Proprietà per gestire la lista di player IDs come array
        [NotMapped]
        public List<string> PlayerIds
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(PlayerIdsJson) 
                        ? new List<string>() 
                        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(PlayerIdsJson) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                PlayerIdsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        // Proprietà calcolate
        [NotMapped]
        public int CurrentPlayerCount => PlayerIds?.Count ?? 0;

        [NotMapped]
        public bool IsFull => CurrentPlayerCount >= MaxPlayers;

        [NotMapped]
        public bool CanStart => CurrentPlayerCount >= 4;

        // Navigazione
        [JsonIgnore]
        public virtual ICollection<PlayerTournament> PlayerTournaments { get; set; } = new List<PlayerTournament>();
        [JsonIgnore]
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
        [JsonIgnore]
        public virtual ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>();
    }
}