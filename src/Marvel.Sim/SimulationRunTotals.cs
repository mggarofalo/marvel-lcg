using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;

using static Marvel.Sim.SimulationRunHarness;
using static Marvel.Sim.SimulationReplayHarness;
using static Marvel.Sim.SimulationReportReader;
using static Marvel.Sim.SimulationHarnessSupport;
namespace Marvel.Sim;

internal sealed class SimulationRunTotals
{
    private int playerWins, villainWins, playerLosses, failures;
    private int rounds, cardsPlayed, playerAttacks, payments, resourceAbilities;
    private readonly Dictionary<string, int> signatures = new(StringComparer.Ordinal);
    internal int Decisions { get; set; }

    internal void AddOutcome(Outcome outcome, string name)
    {
        if (outcome == Outcome.PlayersWin) playerWins++;
        else if (outcome == Outcome.VillainWins) villainWins++;
        else if (outcome == Outcome.PlayersLose) playerLosses++;
        else throw new SimulationRunException($"game ended without a terminal outcome: {name}");
    }

    internal void AddFailure(Exception error)
    {
        failures++;
        string signature = $"{error.GetType().Name}: {error.Message}";
        signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
    }

    internal void Add(Game? game, ActingPolicy? policy)
    {
        if (game is not null) rounds += game.Round;
        if (policy is null) return;
        cardsPlayed += policy.CardsPlayed;
        playerAttacks += policy.PlayerAttacks;
        payments += policy.Payments;
        resourceAbilities += policy.ResourceAbilitiesUsed;
    }

    internal SimulationSummary Summary(int games) => new(
        games, playerWins, villainWins, playerLosses, failures, Decisions, rounds,
        cardsPlayed, playerAttacks, payments, resourceAbilities, signatures);
}
