using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class StealthStrikeReplayReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SettledDefeatReconstructsAcrossStrictJsonAndFreshHostsAtBothChoices(bool side)
    {
        // This is a test-only factory injection, not a public Black Widow setup journey.
        var factory = new StealthStrikeReplayReviewFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "stealth-review", specification));
        Assert.Null(opened.Error);
        var composer = new DecisionComposer(opened.Prompt!);
        composer.SelectAffordance(Assert.Single(opened.Prompt!.Affordances, a => a.AnchorId == factory.Strike).Id);
        foreach (int payment in factory.Payment) composer.ToggleResource(payment);
        Assert.True(composer.TryBuild(out var play, out var error), error);
        var current = Resolve(host, opened, opened, play!);
        current = Resolve(host, opened, current, Decision.Take(factory.Enemy));
        current = Resolve(host, opened, current,
            Decision.Take(Assert.Single(current.Prompt!.Affordances, a => a.AnchorId == factory.Observer).Id));
        Assert.Equal(Question.Option, factory.Game.Pending!.Asking);
        Assert.Equal(DeckType.EncounterDiscardPile, factory.Game.State.Cards[factory.Enemy].Area.Type);
        Assert.Equal(5, factory.Game.State.Cards[factory.Main].Tokens["k_threat"]);

        var uninterrupted = factory.Game;
        var replayed = Replay(factory, store, specification);
        host = Restart(factory, ref store, uninterrupted);
        current = host.Exchange(EngineRequest.SyncGame("response-restart", "stealth-review", opened.Capability!));
        Assert.Null(current.Error);
        var option = Decision.Take(uninterrupted.Pending!.Affordances[0].Id);
        var expected = uninterrupted.Resolve(option);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayed.Resolve(option).Events.Select(EventJson.Write));
        current = Resolve(host, opened, current, option);
        AssertEvents(store, expected.Events);
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(new[] { factory.Main, factory.Side }.Order(), uninterrupted.Pending!.Affordances.Select(a => a.Id).Order());
        Assert.Equal(5, factory.Game.State.Cards[factory.Main].Tokens["k_threat"]);

        // The second restart reconstructs the retained defeat at the scheme choice itself.
        replayed = Replay(factory, store, specification);
        host = Restart(factory, ref store, uninterrupted);
        current = host.Exchange(EngineRequest.SyncGame("scheme-restart", "stealth-review", opened.Capability!));
        Assert.Null(current.Error);
        string before = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var rejected = host.Exchange(EngineRequest.ResolveGame("foreign", "stealth-review", opened.Capability!,
            Decision.Take(factory.Foreign), current.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Equivalent(uninterrupted, factory.Game);
        var scheme = Decision.Take(side ? factory.Side : factory.Main);
        expected = uninterrupted.Resolve(scheme);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayed.Resolve(scheme).Events.Select(EventJson.Write));
        current = Resolve(host, opened, current, scheme);
        AssertEvents(store, expected.Events);
        Equivalent(uninterrupted, replayed);
        Equivalent(uninterrupted, factory.Game);
        Assert.Equal(side ? 5 : 3, factory.Game.State.Cards[factory.Main].Tokens["k_threat"]);
        Assert.Equal(side ? 3 : 5, factory.Game.State.Cards[factory.Side].Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Strike].Area.Type);
        Assert.True(uninterrupted.IsRootPrompt);
        Assert.Equal("complete", Assert.Single(store.Load()).Save.Units[^1].Status);
        Equivalent(uninterrupted, Replay(factory, store, specification));
    }

    private static Game Replay(StealthStrikeReplayReviewFactory factory, MemorySessionStore store, GameSpecification specification) =>
        SessionReplay.Verify(SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save)),
            factory.Compatibility, _ =>
            {
                var fresh = factory.Create(specification);
                return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
            });

    private static EngineHost Restart(StealthStrikeReplayReviewFactory factory, ref MemorySessionStore store, Game expected)
    {
        var copy = StoredSessionJson.Read(StoredSessionJson.Write(Assert.Single(store.Load())));
        string before = SessionSaveJson.Write(copy.Save);
        var restored = new MemorySessionStore();
        Assert.Null(restored.Commit(copy));
        store = restored;
        var host = new EngineHost(factory, store: store);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.NotSame(expected.State, factory.Game.State);
        Equivalent(expected, factory.Game);
        return host;
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision, "stealth-review",
            opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }

    private static void AssertEvents(MemorySessionStore store, IReadOnlyList<GameEvent> expected)
    {
        var step = Assert.Single(store.Load()).Save.Units[^1].Decisions[^1];
        Assert.Equal(expected.Select(JournalJson.Event).Select(value => value.GetRawText()),
            step.Events.Select(value => value.GetRawText()));
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Round, actual.Round);
    }
}
