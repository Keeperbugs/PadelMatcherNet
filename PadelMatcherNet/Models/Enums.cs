using System.ComponentModel.DataAnnotations;

namespace PadelMatcherNet.Models
{
    public enum SkillLevel
    {
        [Display(Name = "Fascia A (Alto)")]
        FasciaA,

        [Display(Name = "Fascia B (Medio/Basso)")]
        FasciaB,

        [Display(Name = "Non Assegnato")]
        Unassigned
    }

    public enum PairingStrategy
    {
        [Display(Name = "Bilanciato A-B")]
        BalancedAB,

        [Display(Name = "Solo Fascia A")]
        SkillA,

        [Display(Name = "Solo Fascia B")]
        SkillB,

        [Display(Name = "Misto")]
        Mixed
    }

    public enum MatchFormat
    {
        [Display(Name = "Al meglio di 3 set")]
        BestOfThree,

        [Display(Name = "Golden Point")]
        GoldenPoint
    }

    public enum MatchStatus
    {
        [Display(Name = "In attesa")]
        Pending,

        [Display(Name = "In corso")]
        InProgress,

        [Display(Name = "Completato")]
        Completed
    }

    public enum TournamentStatus
    {
        [Display(Name = "Bozza")]
        Draft,

        [Display(Name = "Attivo")]
        Active,

        [Display(Name = "Completato")]
        Completed
    }
}