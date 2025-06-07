namespace PadelMatcherNet.Models
{
    // Enum per la modalità di visualizzazione delle classifiche
    public enum ViewMode
    {
        Tournament,  // Classifica specifica di un torneo
        Overall      // Classifica generale (tutti i tornei)
    }

    // Enum per i criteri di ordinamento delle statistiche
    public enum StatsSortBy
    {
        Points,      // Ordina per punti
        MatchesWon,  // Ordina per partite vinte
        WinRate,     // Ordina per percentuale di vittorie
        SetsWon      // Ordina per set vinti
    }

    // Enum per i tipi di esportazione
    public enum ExportType
    {
        Csv,
        Excel,
        Pdf
    }

    // Enum per i periodi di statistiche
    public enum StatsPeriod
    {
        Current,     // Statistiche del torneo corrente
        LastMonth,   // Ultimo mese
        LastThreeMonths, // Ultimi 3 mesi
        AllTime      // Tutto il tempo
    }
}