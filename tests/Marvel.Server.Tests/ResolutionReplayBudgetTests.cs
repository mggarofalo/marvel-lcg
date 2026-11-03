using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>
/// The work one accepted decision costs: a single verifying replay of the
/// live save, whose history equals a fresh replay of the committed save.
/// </summary>
public sealed class ResolutionReplayBudgetTests
{
    private const int DecisionLimit = 200;

    [Fact]
    public void EachAcceptedDecisionDealsOneReplay()
    {
        // Ledger choice: a new decision is constructed on one verified replay of the
        // live save. Its history is collected by that replay, so no second deal
        // replays the proposed save.
        var factory = new CountingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory, store: new MemorySessionStore());
        var accepted = new List<int>();
        Play(host, (_, _, deals) => accepted.Add(deals), () => factory.Deals);

        Assert.True(accepted.Count > 20, $"only {accepted.Count} decisions were accepted");
        Assert.All(accepted, deals => Assert.Equal(1, deals));
    }

    [Fact]
    public void ResolutionHistoryEqualsAFreshReplayOfTheCommittedSaveThroughAnEnding()
    {
        // The game runs to its ending so that open units, completed phase steps and
        // the terminal unit each reach the history a resolution publishes.
        var datasets = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(datasets, store: store);
        EngineResponse last = Play(host, (response, capability, _) =>
        {
            var fresh = new EngineHost(datasets, store: store);
            EngineResponse synced = fresh.Exchange(EngineRequest.SyncGame(
                "fresh-" + response.Revision, response.GameId, capability));
            Assert.Null(synced.Error);
            Assert.Equal(
                JsonSerializer.Serialize(synced.History),
                JsonSerializer.Serialize(response.History));
        }, () => 0);

        Assert.Null(last.Prompt);
        Assert.Contains(Assert.Single(store.Load()).Save.Units, unit => unit.Role == "terminal");
    }

    private static EngineResponse Play(
        EngineHost host,
        Action<EngineResponse, string, int> acceptedWith,
        Func<int> deals)
    {
        EngineResponse current = host.Exchange(EngineRequest.OpenGame(
            "open", "replay-budget",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 12345)));
        Assert.Null(current.Error);
        string capability = current.Capability!;
        for (int step = 0; current.Prompt is Prompt prompt; step++)
        {
            Assert.True(step < DecisionLimit, $"still playing after {step} decisions");
            EngineResponse? accepted = null;
            foreach (Decision decision in Candidates(prompt))
            {
                int before = deals();
                EngineResponse response = host.Exchange(EngineRequest.ResolveGame(
                    "step-" + step, current.GameId, capability, decision, current.Revision));
                if (response.Error is null)
                {
                    accepted = response;
                    acceptedWith(response, capability, deals() - before);
                    break;
                }
            }

            current = accepted ?? throw new InvalidOperationException(
                $"no candidate answer was accepted at decision {step}");
        }

        return current;
    }

    /// <summary>A crude, deterministic policy that keeps the game moving.</summary>
    private static IEnumerable<Decision> Candidates(Prompt prompt)
    {
        foreach (Affordance card in prompt.Affordances.Where(offer =>
                     offer.Verb == CardPlay.Verb && offer.CostOptions.Count > 0))
        {
            if (Payment(card) is { } paying
                && ResourcePayment.Allocate(card.CostOptions[0], paying) is { } allocation)
            {
                yield return Decision.Take(card.Id,
                    card.Targets is { Legal.Count: > 0 } targets ? [targets.Legal[0]] : [],
                    paying, new Dictionary<string, long>(), allocation);
            }
        }

        if (prompt.Asking == Question.Defender)
            yield return Decision.Take(prompt.Affordances[^1].Id);
        foreach (string verb in new[] { BasicPowers.ThwartVerb, BasicPowers.AttackVerb, Game.ChangeForm, Game.EndPhaseVerb })
        {
            foreach (Affordance offer in prompt.Affordances.Where(offer => offer.Verb == verb))
                yield return Answer(prompt, offer);
        }

        if (prompt.Cancellable)
            yield return Decision.Decline;
        foreach (Affordance offer in prompt.Affordances.Where(offer => offer.Verb != CardPlay.Verb))
            yield return Answer(prompt, offer);
    }

    private static Decision Answer(Prompt prompt, Affordance offer)
    {
        if (offer.Targets is not { } targets)
            return Decision.Take(offer.Id);
        if (prompt.Asking == Question.Order)
            return new Decision(offer.Id, [.. targets.Legal]);
        int count = offer.Verb == Game.EndPhaseVerb
            ? targets.Min
            : Math.Max(targets.Min, Math.Min(1, targets.Legal.Count));
        return new Decision(offer.Id, [.. targets.Legal.Take(count)]);
    }

    private static int[]? Payment(Affordance offer)
    {
        if (!long.TryParse(offer.CostOptions[0].Cost,
                System.Globalization.CultureInfo.InvariantCulture, out long cost))
            return null;
        var spent = new List<int>();
        long generated = 0;
        foreach (var source in offer.CostOptions[0].Generators)
        {
            if (generated >= cost)
                break;
            spent.Add(source.Effect);
            generated += source.Generates.Length;
        }

        return generated >= cost ? [.. spent] : null;
    }

    private sealed class CountingFactory(DatasetGameFactory inner) : IDurableGameFactory
    {
        public int Deals { get; private set; }

        public SessionCompatibility Compatibility => inner.Compatibility;

        public OpenedGame Create(GameSpecification specification)
        {
            Deals++;
            return inner.Create(specification);
        }
    }
}
