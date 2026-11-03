using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>
/// The work one accepted decision costs: construction on the session's verified
/// checkpoint, or a single verifying replay of the live save when there is none,
/// with a history equal to a fresh replay of the committed save.
/// </summary>
public sealed class ResolutionReplayBudgetTests
{
    private const int DecisionLimit = 200;

    [Fact]
    public void AGameInProgressConstructsEachDecisionOnItsCheckpoint()
    {
        // Ledger choice: the first decision of a published session replays the live
        // save once and leaves its candidate as the checkpoint. Every later decision
        // constructs on the checkpoint its predecessor left, so the work a decision
        // costs does not grow with the length of the game.
        var factory = new CountingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory, store: new MemorySessionStore());
        var table = Table.Open(host, "replay-budget");
        var deals = new List<int>();
        while (table.Current.Prompt is not null)
        {
            int before = factory.Deals;
            table.Accept();
            deals.Add(factory.Deals - before);
        }

        Assert.True(deals.Count > 20, $"only {deals.Count} decisions were accepted");
        Assert.Equal(1, deals[0]);
        Assert.All(deals.Skip(1), dealt => Assert.Equal(0, dealt));
    }

    [Fact]
    public void HistoryEditingLeavesTheNextDecisionToReplayTheSave()
    {
        // A checkpoint answers only the save it was made for. Undo publishes a
        // save whose cursor stops short of its redo suffix, so the next decision
        // verifies the whole retained trace and then deals the active prefix: two
        // replays. The decision after constructs on the checkpoint they left.
        var factory = new CountingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var table = Table.Open(host, "edit-budget");
        while (table.Accepted < 8 || !Undoable(store))
        {
            table.Accept();
        }

        int cursor = Assert.Single(store.Load()).Save.Cursor;
        EngineResponse undone = host.Exchange(EngineRequest.UndoGame(
            "undo", table.Current.GameId, table.Capability, cursor - 1, table.Current.Revision));
        Assert.Null(undone.Error);
        table.Continue(undone);

        int before = factory.Deals;
        table.Accept();
        Assert.Equal(2, factory.Deals - before);
        before = factory.Deals;
        table.Accept();
        Assert.Equal(0, factory.Deals - before);
    }

    [Fact]
    public void AFailedCommitRestoresTheLiveGameAndGivesUpTheCheckpoint()
    {
        // The live game reproduces a decision before it is committed, so a failed
        // commit must leave the live game verified at the live save. The checkpoint
        // was spent on the failed candidate, so the retried decision replays once.
        var factory = new CountingFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var inner = new MemorySessionStore();
        var host = new EngineHost(factory, store: new FailingStore(inner, failAtCommit: 6));
        var table = Table.Open(host, "failed-commit");
        for (int accepted = 0; accepted < 4; accepted++)
        {
            table.Accept();
        }

        EngineResponse before = table.Sync("before");
        Assert.Equal("save_failed", table.Attempt().Error?.Code);
        EngineResponse after = table.Sync("after");
        Assert.Equal(EngineJson.Write(before with { RequestId = "sync" }),
            EngineJson.Write(after with { RequestId = "sync" }));

        int dealt = factory.Deals;
        table.Accept();
        Assert.Equal(1, factory.Deals - dealt);
        dealt = factory.Deals;
        table.Accept();
        Assert.Equal(0, factory.Deals - dealt);
        AssertFreshReplayAgrees(DatasetGameFactory.Load(RepositoryPaths.Root), inner, table);
    }

    [Fact]
    public void ResolutionHistoryEqualsAFreshReplayOfTheCommittedSaveThroughAnEnding()
    {
        // The game runs to its ending so that open units, completed phase steps and
        // the terminal unit each reach the history a resolution publishes.
        var datasets = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var table = Table.Open(new EngineHost(datasets, store: store), "replay-history");
        while (table.Current.Prompt is not null)
        {
            table.Accept();
            AssertFreshReplayAgrees(datasets, store, table);
        }

        Assert.Contains(Assert.Single(store.Load()).Save.Units, unit => unit.Role == "terminal");
    }

    private static void AssertFreshReplayAgrees(
        DatasetGameFactory datasets, ISessionStore store, Table table)
    {
        var fresh = new EngineHost(datasets, store: store);
        EngineResponse synced = fresh.Exchange(EngineRequest.SyncGame(
            "fresh-" + table.Current.Revision, table.Current.GameId, table.Capability));
        Assert.Null(synced.Error);
        Assert.Equal(
            JsonSerializer.Serialize(synced.History),
            JsonSerializer.Serialize(table.Current.History));
        Assert.Equal(
            JsonSerializer.Serialize(synced.Prompt),
            JsonSerializer.Serialize(table.Current.Prompt));
    }

    /// <summary>The last unit is complete and revealed no new information.</summary>
    private static bool Undoable(MemorySessionStore store)
    {
        SessionSave save = Assert.Single(store.Load()).Save;
        return save.Units is [.., { Status: "complete" }] && save.EditFrontier < save.Cursor;
    }

    /// <summary>One seat's view of a hosted game, advanced by the crude policy.</summary>
    private sealed class Table(EngineHost host, EngineResponse opened)
    {
        public EngineResponse Current { get; private set; } = opened;

        public string Capability { get; } = opened.Capability!;

        public int Accepted { get; private set; }

        public static Table Open(EngineHost host, string gameId)
        {
            EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
                "open", gameId,
                new GameSpecification("rhino", ["spider_man"], [], Seed: 12345)));
            Assert.Null(opened.Error);
            return new Table(host, opened);
        }

        public void Continue(EngineResponse response) => Current = response;

        public EngineResponse Sync(string requestId) =>
            host.Exchange(EngineRequest.SyncGame(requestId, Current.GameId, Capability));

        /// <summary>Answers with the first candidate the host accepts.</summary>
        public void Accept()
        {
            Assert.True(Accepted < DecisionLimit, $"still playing after {Accepted} decisions");
            EngineResponse response = Attempt();
            Assert.Null(response.Error);
            Current = response;
            Accepted++;
        }

        /// <summary>The first response that accepts a candidate or fails to save it.</summary>
        public EngineResponse Attempt()
        {
            Prompt prompt = Current.Prompt ?? throw new InvalidOperationException("the game is over");
            EngineResponse? last = null;
            foreach (Decision decision in Candidates(prompt))
            {
                last = host.Exchange(EngineRequest.ResolveGame(
                    "step-" + Accepted, Current.GameId, Capability, decision, Current.Revision));
                if (last.Error is null || last.Error.Code == "save_failed")
                {
                    return last;
                }
            }

            throw new InvalidOperationException(
                $"no candidate answer was accepted at decision {Accepted}: {last?.Error?.Code}");
        }
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

    private sealed class FailingStore(ISessionStore inner, int failAtCommit) : ISessionStore
    {
        private int commits;

        public IReadOnlyList<StoredSession> Load() => inner.Load();

        public string? Commit(StoredSession session) =>
            ++commits == failAtCommit
                ? throw new IOException("simulated interrupted write")
                : inner.Commit(session);
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
