using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ThorEntryReplayReviewTests
{
    [Theory]
    [InlineData('Y', 3)]
    [InlineData('R', 2)]
    public void ActualValkyrieReplaysOriginalDeclaredPriceThroughEntryAndTarget(char declaration, int damage)
    {
        var factory = new ThorEntryReplayReviewFactory("06012");
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Open(host);
        var current = Resolve(host, opened, opened, Play(opened.Prompt!, factory, declaration));
        var control = factory.Game;
        host = Restart(factory, ref store, control);
        current = host.Exchange(EngineRequest.SyncGame("response-restart", "entry-review", opened.Capability!));
        Assert.Null(current.Error);
        var response = Assert.Single(current.Prompt!.Affordances, a => a.AnchorId == factory.Ally);
        var expected = control.Resolve(Decision.Take(response.Id));
        current = Resolve(host, opened, current, Decision.Take(response.Id));
        AssertEvents(store, expected.Events);
        Equivalent(control, factory.Game);
        host = Restart(factory, ref store, control);
        current = host.Exchange(EngineRequest.SyncGame("target-restart", "entry-review", opened.Capability!));
        Assert.Null(current.Error);
        expected = control.Resolve(Decision.Take(factory.Minion));
        current = Resolve(host, opened, current, Decision.Take(factory.Minion));
        AssertEvents(store, expected.Events);
        Equivalent(control, factory.Game);
        Assert.Equal(damage, factory.Game.State.Cards[factory.Minion].Damage);
        Assert.True(factory.Game.IsRootPrompt);
        Assert.Equal("complete", Assert.Single(store.Load()).Save.Units[^1].Status);
        Restart(factory, ref store, control);
    }

    [Fact]
    [Rule("rr:discard.5")]
    public void ActualHeimdallReplaysFrozenGroupRejectsForgedOrderAndReturnsChosenPhysicalCards()
    {
        var factory = new ThorEntryReplayReviewFactory("06020");
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Open(host);
        var current = Resolve(host, opened, opened, Play(opened.Prompt!, factory, 'R'));
        var response = Assert.Single(current.Prompt!.Affordances, a => a.AnchorId == factory.Ally);
        current = Resolve(host, opened, current, Decision.Take(response.Id));
        Assert.Empty(current.Prompt!.ContextCardIds);
        current = Resolve(host, opened, current, Decision.Take(Assert.Single(current.Prompt.Affordances).Id));
        // A looked-at selected card is discarded "from the top of that deck".
        int[] group = current.Prompt!.ContextCardIds.ToArray();
        Assert.Equal(3, group.Length);
        var control = factory.Game;
        host = Restart(factory, ref store, control);
        current = host.Exchange(EngineRequest.SyncGame("discard-restart", "entry-review", opened.Capability!));
        Assert.Null(current.Error);
        control.Resolve(Decision.Take(group[1]));
        current = Resolve(host, opened, current, Decision.Take(group[1]));
        Equivalent(control, factory.Game);
        Assert.Equal(Question.Order, current.Prompt!.Asking);
        host = Restart(factory, ref store, control);
        current = host.Exchange(EngineRequest.SyncGame("order-restart", "entry-review", opened.Capability!));
        Assert.Null(current.Error);
        string saved = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var invalid = new Decision(Assert.Single(current.Prompt!.Affordances).Id, [group[0], group[0]]);
        var rejected = host.Exchange(EngineRequest.ResolveGame("duplicate", "entry-review", opened.Capability!, invalid, current.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Empty(rejected.Events);
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Equivalent(control, factory.Game);
        var composer = new DecisionComposer(current.Prompt);
        composer.SelectAffordance(Assert.Single(current.Prompt.Affordances).Id);
        composer.SelectTargets([group[2], group[0]]);
        Assert.True(composer.TryBuild(out var answer, out var error), error);
        var expected = control.Resolve(answer!);
        current = Resolve(host, opened, current, answer!);
        AssertEvents(store, expected.Events);
        Equivalent(control, factory.Game);
        Assert.Equal(DeckType.EncounterDiscardPile, factory.Game.State.Cards[group[1]].Area.Type);
        Assert.Equal(new[] { group[2], group[0] }, factory.Game.State.AreaOf(DeckType.EncounterDeck)
            .Cards.Reverse().Take(2).Select(c => c.ObjectId));
        Assert.True(factory.Game.IsRootPrompt);
        Restart(factory, ref store, control);
    }

    private static EngineResponse Open(EngineHost host)
    {
        var result = host.Exchange(EngineRequest.OpenGame("open", "entry-review",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        Assert.Null(result.Error);
        return result;
    }

    private static Decision Play(Prompt prompt, ThorEntryReplayReviewFactory factory, char declaration)
    {
        var composer = new DecisionComposer(prompt);
        composer.SelectAffordance(Assert.Single(prompt.Affordances, a => a.AnchorId == factory.Ally).Id);
        foreach (int id in factory.Payment) composer.ToggleResource(id);
        if (factory.Payment.Length == 2)
        {
            composer.AssignResource(factory.Payment[0], 0, 0, declaration);
            composer.AssignResource(factory.Payment[0], 1, 0, declaration);
        }
        Assert.True(composer.TryBuild(out var answer, out var error), error);
        return answer!;
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision, "entry-review",
            opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }

    private static EngineHost Restart(ThorEntryReplayReviewFactory factory, ref MemorySessionStore store, Game expected)
    {
        var copy = StoredSessionJson.Read(StoredSessionJson.Write(Assert.Single(store.Load())));
        string before = SessionSaveJson.Write(copy.Save);
        var restored = new MemorySessionStore();
        Assert.Null(restored.Commit(copy));
        store = restored;
        var host = new EngineHost(factory, store: store);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(restored.Load()).Save));
        Equivalent(expected, factory.Game);
        return host;
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
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)),
            JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}
