using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class NonattackPreventionReplayReviewTests
{
    private const string Label = "nonattack-prevention-review";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:damage.step.3")]
    [Rule("rr:prevent.1")]
    public void PendingPreventionAndItsSuspendedCompletionSurviveStrictJsonFreshHosts(bool simultaneous, bool barrier)
    {
        // "when [character] would take any amount of damage" precedes placement;
        // prevention must "reduce the amount of damage the target takes" after restart too.
        var factory = new NonattackPreventionReplayReviewFactory(simultaneous, barrier);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Exchange(host, EngineRequest.OpenGame("open", Label,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        var pending = Resolve(host, opened, opened, Take(opened, factory.Action), "damage");
        Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Prevention);
        Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, factory.Game.State.Cards[factory.Ally].Damage);
        var control = factory.Game;
        (host, factory, store, pending) = Restart(simultaneous, barrier, opened, pending, store, control);

        string archive = StoredSessionJson.Write(Assert.Single(store.Load()));
        var rejected = ExchangeRaw(host, EngineRequest.ResolveGame("invalid-target", Label, opened.Capability!,
            new Decision(Take(pending, factory.Prevention).Affordance, [factory.Ally]), pending.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Empty(rejected.Events);
        Assert.Equal(archive, StoredSessionJson.Write(Assert.Single(store.Load())));
        Equivalent(control, factory.Game);

        pending = Continue(host, opened, pending, Take(pending, factory.Prevention), "prevent", control, factory);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Prevention].Area.Type);
        Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
        if (barrier)
        {
            Assert.Equal(Question.Element, pending.Prompt!.Asking);
            Assert.False(factory.Game.IsRootPrompt);
        }
        else
        {
            Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Suit);
            Assert.Equal("08032", factory.Game.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility!.Face);
            Assert.Equal(0, factory.Game.State.Cards[factory.Ally].Damage);
        }
        (host, factory, store, pending) = Restart(simultaneous, barrier, opened, pending, store, control);

        if (barrier)
        {
            var target = Assert.Single(pending.Prompt!.Affordances, a => a.Id == factory.Villain);
            pending = Continue(host, opened, pending, Decision.Take(target.Id), "reflect", control, factory);
        }
        else
        {
            pending = Continue(host, opened, pending, Take(pending, factory.Suit), "ready", control, factory);
            Assert.True(factory.Game.State.Seats[0].IdentityCard.Ready);
            Assert.False(factory.Game.State.Cards[factory.Suit].Ready);
            pending = Continue(host, opened, pending, Decision.Decline, "decline-widowmaker", control, factory);
        }
        Assert.True(factory.Game.IsRootPrompt);
        Assert.False(factory.Game.State.IsOver);
        Assert.Equal(barrier ? 1 : 0, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(simultaneous ? 2 : 0, factory.Game.State.Cards[factory.Ally].Damage);
        Assert.Equal(barrier ? 1 : 0, factory.Game.State.Cards[factory.Villain].Damage);
        Assert.Null(factory.Game.State.Activation);
        _ = Restart(simultaneous, barrier, opened, pending, store, control);
    }

    private static (EngineHost Host, NonattackPreventionReplayReviewFactory Factory, MemorySessionStore Store, EngineResponse Pending)
        Restart(bool simultaneous, bool barrier, EngineResponse opened, EngineResponse pending, MemorySessionStore store, Game control)
    {
        string archive = StoredSessionJson.Write(Assert.Single(store.Load()));
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(StoredSessionJson.Read(archive)));
        var factory = new NonattackPreventionReplayReviewFactory(simultaneous, barrier);
        var host = new EngineHost(factory, store: restoredStore);
        var restored = Exchange(host, EngineRequest.SyncGame("restart", Label, opened.Capability!));
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(archive, StoredSessionJson.Write(Assert.Single(restoredStore.Load())));
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        return (host, factory, restoredStore, restored);
    }

    private static EngineResponse Continue(EngineHost host, EngineResponse opened, EngineResponse pending,
        Decision answer, string id, Game control, NonattackPreventionReplayReviewFactory factory)
    {
        var expected = control.Resolve(answer);
        var result = Resolve(host, opened, pending, answer, id);
        Assert.Equal(expected.Events.Select(EventJson.Write), result.Events.Select(EventJson.Write));
        Equivalent(control, factory.Game);
        return result;
    }

    private static Decision Take(EngineResponse response, int source) =>
        Decision.Take(Assert.Single(response.Prompt!.Affordances, a => a.AnchorId == source).Id);

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current,
        Decision answer, string id) => Exchange(host,
            EngineRequest.ResolveGame(id, Label, opened.Capability!, answer, current.Revision));

    private static EngineResponse Exchange(EngineHost host, EngineRequest request)
    {
        var response = ExchangeRaw(host, request);
        Assert.Null(response.Error);
        return response;
    }

    private static EngineResponse ExchangeRaw(EngineHost host, EngineRequest request) =>
        EngineJson.ReadResponse(EngineJson.Write(host.Exchange(EngineJson.ReadRequest(EngineJson.Write(request)))));

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
