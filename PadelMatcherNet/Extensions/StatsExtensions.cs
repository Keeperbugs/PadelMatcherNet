using PadelMatcherNet.Models;

namespace PadelMatcherNet.Extensions
{
    public static class StatsExtensions
    {
        /// <summary>
        /// Calcola la percentuale di vittorie per un giocatore
        /// </summary>
        public static double GetWinRate(this PlayerStats stats)
        {
            if (stats.MatchesPlayed == 0) return 0.0;
            return (double)stats.MatchesWon / stats.MatchesPlayed * 100.0;
        }

        /// <summary>
        /// Calcola il quoziente set per un giocatore
        /// </summary>
        public static double GetSetRatio(this PlayerStats stats)
        {
            if (stats.SetsLost == 0)
                return stats.SetsWon > 0 ? double.PositiveInfinity : 0.0;
            
            return (double)stats.SetsWon / stats.SetsLost;
        }

        /// <summary>
        /// Calcola il quoziente game per un giocatore
        /// </summary>
        public static double GetGameRatio(this PlayerStats stats)
        {
            if (stats.GamesLost == 0)
                return stats.GamesWon > 0 ? double.PositiveInfinity : 0.0;
            
            return (double)stats.GamesWon / stats.GamesLost;
        }

        /// <summary>
        /// Restituisce una stringa formattata per il quoziente set
        /// </summary>
        public static string GetSetRatioDisplay(this PlayerStats stats)
        {
            var ratio = stats.GetSetRatio();
            
            if (double.IsPositiveInfinity(ratio))
                return "∞";
            
            if (ratio == 0.0)
                return "0.00";
            
            return ratio.ToString("F2");
        }

        /// <summary>
        /// Restituisce una stringa formattata per la percentuale di vittorie
        /// </summary>
        public static string GetWinRateDisplay(this PlayerStats stats)
        {
            return stats.GetWinRate().ToString("F1") + "%";
        }

        /// <summary>
        /// Determina se un giocatore è "in forma" (ultime 5 partite con buoni risultati)
        /// </summary>
        public static bool IsInGoodForm(this PlayerStats stats)
        {
            // Logica semplificata: considera in forma se ha almeno 3 partite giocate 
            // e una percentuale di vittorie superiore al 60%
            return stats.MatchesPlayed >= 3 && stats.GetWinRate() >= 60.0;
        }

        /// <summary>
        /// Calcola il punteggio medio per partita
        /// </summary>
        public static double GetAveragePointsPerMatch(this PlayerStats stats)
        {
            if (stats.MatchesPlayed == 0) return 0.0;
            return (double)stats.Points / stats.MatchesPlayed;
        }

        /// <summary>
        /// Restituisce il livello di performance basato sui punti
        /// </summary>
        public static PerformanceLevel GetPerformanceLevel(this PlayerStats stats)
        {
            var winRate = stats.GetWinRate();
            
            if (winRate >= 80.0) return PerformanceLevel.Excellent;
            if (winRate >= 65.0) return PerformanceLevel.VeryGood;
            if (winRate >= 50.0) return PerformanceLevel.Good;
            if (winRate >= 35.0) return PerformanceLevel.Average;
            return PerformanceLevel.Poor;
        }

        /// <summary>
        /// Confronta due giocatori per l'ordinamento in classifica
        /// </summary>
        public static int CompareForRanking(this PlayerStats stats1, PlayerStats stats2, StatsSortBy sortBy = StatsSortBy.Points)
        {
            // Ordinamento primario in base al criterio selezionato
            int primaryComparison = sortBy switch
            {
                StatsSortBy.Points => stats2.Points.CompareTo(stats1.Points),
                StatsSortBy.MatchesWon => stats2.MatchesWon.CompareTo(stats1.MatchesWon),
                StatsSortBy.WinRate => stats2.GetWinRate().CompareTo(stats1.GetWinRate()),
                StatsSortBy.SetsWon => stats2.SetsWon.CompareTo(stats1.SetsWon),
                _ => stats2.Points.CompareTo(stats1.Points)
            };

            if (primaryComparison != 0) return primaryComparison;

            // Criteri di spareggio in ordine di priorità
            // 1. Partite vinte
            int matchesComparison = stats2.MatchesWon.CompareTo(stats1.MatchesWon);
            if (matchesComparison != 0) return matchesComparison;

            // 2. Quoziente set
            int setRatioComparison = stats2.GetSetRatio().CompareTo(stats1.GetSetRatio());
            if (setRatioComparison != 0) return setRatioComparison;

            // 3. Set vinti
            int setsWonComparison = stats2.SetsWon.CompareTo(stats1.SetsWon);
            if (setsWonComparison != 0) return setsWonComparison;

            // 4. Nome (ordine alfabetico)
            var name1 = stats1.Player?.FullName ?? "";
            var name2 = stats2.Player?.FullName ?? "";
            return name1.CompareTo(name2);
        }
    }

    /// <summary>
    /// Enum per il livello di performance di un giocatore
    /// </summary>
    public enum PerformanceLevel
    {
        Poor,       // Scarso (< 35%)
        Average,    // Medio (35-50%)
        Good,       // Buono (50-65%)
        VeryGood,   // Molto buono (65-80%)
        Excellent   // Eccellente (>= 80%)
    }
}