using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record SummaryRecord(
    string Type,
    int Games,
    int PlayersWin,
    int VillainWins,
    int PlayersLose,
    int Failures,
    int Decisions,
    int Rounds,
    int CardsPlayed,
    int PlayerAttacks,
    int Payments,
    int ResourceAbilitiesUsed,
    IReadOnlyDictionary<string, int> FailureSignatures);
