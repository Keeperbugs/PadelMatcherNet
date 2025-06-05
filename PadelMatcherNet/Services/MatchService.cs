using Microsoft.EntityFrameworkCore;
using PadelMatcherNet.Data;
using PadelMatcherNet.Models;

namespace PadelMatcherNet.Services
{
    public interface IMatchService
    {
        Task<List<Match>> GetMatchesByTournamentAsync(string tournamentId);
        Task<Match?> GetMatchByIdAsync(string matchId);
        Task<Match> CreateMatchAsync(Match match);
        Task<Match> UpdateMatchAsync(Match match);
        Task<bool> DeleteMatchAsync(string matchId);
        Task<Match> SaveMatchResultsAsync(string matchId, List<MatchSetScore> scores, string? winnerTeamId = null);
        Task<List<Match>> GenerateMatchesAsync(string tournamentId, PairingStrategy strategy, MatchFormat format);
        Task<bool> DeleteUncompletedMatchesAsync(string tournamentId);
        Task<Match> CreateManualMatchAsync(string tournamentId, Team team1, Team team2, MatchFormat format, string? court = null);
    }

    public class MatchService : IMatchService
    {
        private readonly TournamentDbContext _context;
        private readonly ITournamentService _tournamentService;

        public MatchService(TournamentDbContext context, ITournamentService tournamentService)
        {
            _context = context;
            _tournamentService = tournamentService;
        }

        public async Task<List<Match>> GetMatchesByTournamentAsync(string tournamentId)
        {
            return await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Match?> GetMatchByIdAsync(string matchId)
        {
            return await _context.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        }

        public async Task<Match> CreateMatchAsync(Match match)
        {
            // Genera nuovo ID se non specificato
            if (string.IsNullOrEmpty(match.Id))
            {
                match.Id = Guid.NewGuid().ToString();
            }

            // Imposta i timestamp
            match.CreatedAt = DateTime.UtcNow;
            match.UpdatedAt = DateTime.UtcNow;

            // Valori di default
            if (match.Scores == null)
            {
                match.Scores = new List<MatchSetScore>();
            }

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            return match;
        }

        public async Task<Match> UpdateMatchAsync(Match match)
        {
            var existingMatch = await _context.Matches.FindAsync(match.Id);
            if (existingMatch == null)
            {
                throw new ArgumentException($"Match with ID {match.Id} not found");
            }

            // Aggiorna i campi
            existingMatch.Round = match.Round;
            existingMatch.Team1 = match.Team1;
            existingMatch.Team2 = match.Team2;
            existingMatch.Scores = match.Scores;
            existingMatch.Status = match.Status;
            existingMatch.MatchFormat = match.MatchFormat;
            existingMatch.Court = match.Court;
            existingMatch.WinnerTeamId = match.WinnerTeamId;
            existingMatch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingMatch;
        }

        public async Task<bool> DeleteMatchAsync(string matchId)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null)
            {
                return false;
            }

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Match> SaveMatchResultsAsync(string matchId, List<MatchSetScore> scores, string? winnerTeamId = null)
        {
            var match = await GetMatchByIdAsync(matchId);
            if (match == null)
            {
                throw new ArgumentException($"Match with ID {matchId} not found");
            }

            match.Scores = scores;
            match.WinnerTeamId = winnerTeamId;
            match.Status = !string.IsNullOrEmpty(winnerTeamId) ? MatchStatus.Completed : MatchStatus.InProgress;
            match.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<List<Match>> GenerateMatchesAsync(string tournamentId, PairingStrategy strategy, MatchFormat format)
        {
            var tournament = await _tournamentService.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                throw new ArgumentException($"Tournament with ID {tournamentId} not found");
            }

            // Verifica che non ci siano partite in corso
            var uncompletedMatches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId && m.Status != MatchStatus.Completed)
                .ToListAsync();

            if (uncompletedMatches.Any())
            {
                throw new InvalidOperationException("Cannot generate new matches while there are uncompleted matches");
            }

            // Ottieni i giocatori del torneo
            var players = await _tournamentService.GetTournamentPlayersAsync(tournamentId);
            if (players.Count < 4)
            {
                throw new InvalidOperationException("At least 4 players are required to generate matches");
            }

            // Dividi i giocatori per fascia
            var fasciaAPlayers = players.Where(p => p.SkillLevel == SkillLevel.FasciaA).ToList();
            var fasciaBPlayers = players.Where(p => p.SkillLevel == SkillLevel.FasciaB).ToList();
            var unassignedPlayers = players.Where(p => p.SkillLevel == SkillLevel.Unassigned).ToList();

            // Mescola i giocatori
            fasciaAPlayers = ShufflePlayers(fasciaAPlayers);
            fasciaBPlayers = ShufflePlayers(fasciaBPlayers);
            unassignedPlayers = ShufflePlayers(unassignedPlayers);

            // Crea le squadre in base alla strategia
            var teams = CreateTeams(strategy, fasciaAPlayers, fasciaBPlayers, unassignedPlayers);

            if (teams.Count < 2)
            {
                throw new InvalidOperationException("Not enough teams could be formed with the selected strategy");
            }

            // Crea le partite
            var matches = new List<Match>();
            for (int i = 0; i < teams.Count - 1; i += 2)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournamentId,
                    Round = tournament.CurrentRound,
                    Team1 = teams[i],
                    Team2 = teams[i + 1],
                    Scores = new List<MatchSetScore>(),
                    Status = MatchStatus.Pending,
                    MatchFormat = format,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                matches.Add(match);
            }

            // Salva le partite
            _context.Matches.AddRange(matches);

            // Incrementa il round corrente del torneo
            tournament.CurrentRound += 1;
            await _tournamentService.UpdateTournamentAsync(tournament);

            await _context.SaveChangesAsync();

            return matches;
        }

        public async Task<bool> DeleteUncompletedMatchesAsync(string tournamentId)
        {
            var uncompletedMatches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId && m.Status != MatchStatus.Completed)
                .ToListAsync();

            if (uncompletedMatches.Any())
            {
                _context.Matches.RemoveRange(uncompletedMatches);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<Match> CreateManualMatchAsync(string tournamentId, Team team1, Team team2, MatchFormat format, string? court = null)
        {
            var tournament = await _tournamentService.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                throw new ArgumentException($"Tournament with ID {tournamentId} not found");
            }

            var match = new Match
            {
                Id = Guid.NewGuid().ToString(),
                TournamentId = tournamentId,
                Round = tournament.CurrentRound,
                Team1 = team1,
                Team2 = team2,
                Scores = new List<MatchSetScore>(),
                Status = MatchStatus.Pending,
                MatchFormat = format,
                Court = court,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            return match;
        }

        private List<Player> ShufflePlayers(List<Player> players)
        {
            var random = new Random();
            return players.OrderBy(x => random.Next()).ToList();
        }

        private List<Team> CreateTeams(PairingStrategy strategy, List<Player> fasciaA, List<Player> fasciaB, List<Player> unassigned)
        {
            var teams = new List<Team>();

            switch (strategy)
            {
                case PairingStrategy.BalancedAB:
                    // Crea squadre con un giocatore A e un giocatore B
                    while (fasciaA.Any() && fasciaB.Any())
                    {
                        var team = new Team
                        {
                            Id = Guid.NewGuid().ToString(),
                            Player1 = fasciaA.First(),
                            Player2 = fasciaB.First()
                        };
                        teams.Add(team);
                        fasciaA.RemoveAt(0);
                        fasciaB.RemoveAt(0);
                    }
                    break;

                case PairingStrategy.SkillA:
                    // Crea squadre solo con giocatori di fascia A
                    while (fasciaA.Count >= 2)
                    {
                        var team = new Team
                        {
                            Id = Guid.NewGuid().ToString(),
                            Player1 = fasciaA[0],
                            Player2 = fasciaA[1]
                        };
                        teams.Add(team);
                        fasciaA.RemoveRange(0, 2);
                    }
                    break;

                case PairingStrategy.SkillB:
                    // Crea squadre solo con giocatori di fascia B
                    while (fasciaB.Count >= 2)
                    {
                        var team = new Team
                        {
                            Id = Guid.NewGuid().ToString(),
                            Player1 = fasciaB[0],
                            Player2 = fasciaB[1]
                        };
                        teams.Add(team);
                        fasciaB.RemoveRange(0, 2);
                    }
                    break;

                case PairingStrategy.Mixed:
                    // Mescola tutti i giocatori e crea squadre
                    var allPlayers = new List<Player>();
                    allPlayers.AddRange(fasciaA);
                    allPlayers.AddRange(fasciaB);
                    allPlayers.AddRange(unassigned);
                    allPlayers = ShufflePlayers(allPlayers);

                    while (allPlayers.Count >= 2)
                    {
                        var team = new Team
                        {
                            Id = Guid.NewGuid().ToString(),
                            Player1 = allPlayers[0],
                            Player2 = allPlayers[1]
                        };
                        teams.Add(team);
                        allPlayers.RemoveRange(0, 2);
                    }
                    break;
            }

            return teams;
        }
    }
}