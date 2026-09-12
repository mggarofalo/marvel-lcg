using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record SimulationSummary(
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
    IReadOnlyDictionary<string, int> FailureSignatures)
{
    public int ExitCode => Failures == 0 ? 0 : 1;

    public string Human(string? path = null)
    {
        string destination = path is null ? string.Empty : $" Records: {path}.";
        return $"{Games} game(s): {PlayersWin} player win(s), {VillainWins} villain win(s), "
            + $"{PlayersLose} player-loss ending(s), {Failures} failure(s), "
            + $"{Decisions} decision(s), {Rounds} total round(s), {CardsPlayed} card(s) played, "
            + $"{PlayerAttacks} player attack(s), {Payments} paid cost(s).{destination}";
    }
}
