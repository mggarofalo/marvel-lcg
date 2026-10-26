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

public sealed class AttacrobaticsReplayReviewTests
{
    private const string Label = "attacrobatics-review";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.2")]
    public void BoostAndPartialAttackReceiptSurviveStrictJsonFreshHosts(bool guard)
    {
        // "The engaged player cannot attack any villain."
        // "that ability is considered to be an attack made by that player's identity."
        // Ruling 0fc2a7cc9197e127: cancellation under Guard has no attacked enemy.
        var factory = new AttacrobaticsReplayReviewFactory(guard);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Exchange(host, EngineRequest.OpenGame("open", Label,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        var pending = Resolve(host, opened, opened, Take(opened, factory.Action), "attack");
        Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Preparation);
        Assert.NotNull(factory.Game.State.Activation);
        var control = factory.Game;
        (host, factory, store, pending) = Restart(guard, opened, pending, store, control);

        string before = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var rejected = ExchangeRaw(host, EngineRequest.ResolveGame("invalid-target", Label, opened.Capability!,
            new Decision(Take(pending, factory.Preparation).Affordance, [factory.Villain]), pending.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Empty(rejected.Events);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(EngineJson.Write(pending with { RequestId = "same" }), EngineJson.Write(
            Exchange(host, EngineRequest.SyncGame("same", Label, opened.Capability!))));

        var answer = Take(pending, factory.Preparation);
        control.Resolve(answer);
        pending = Resolve(host, opened, pending, answer, "cancel-icons");
        Equivalent(control, factory.Game);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Preparation].Area.Type);
        Assert.True(factory.Game.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility is ResolvedPreparationAbility,
            JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)));
        Assert.Equal(guard ? 0 : 2, factory.Game.State.Cards[factory.Villain].Damage);
        Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Suit);
        Assert.DoesNotContain(pending.Prompt.Affordances, a => a.AnchorId == factory.Game.State.Seats[0].IdentityCard.ObjectId);
        var start = Assert.Single(pending.Events.OfType<AttackBoundary>(), e => e.Source == control.State.Seats[0].IdentityCard.ObjectId && e.Started);
        Assert.Equal(guard ? -1 : factory.Villain, start.Target);
        Assert.All(pending.Events.OfType<AttackBoundary>(), e => Assert.DoesNotContain(-1, e.Subjects!.Keys));
        (host, factory, store, pending) = Restart(guard, opened, pending, store, control);

        answer = Take(pending, factory.Suit);
        control.Resolve(answer);
        pending = Resolve(host, opened, pending, answer, "suit");
        Equivalent(control, factory.Game);
        Assert.True(factory.Game.State.Seats[0].IdentityCard.Ready);
        Assert.False(factory.Game.State.Cards[factory.Suit].Ready);
        var occurrence = factory.Game.State.Agenda.Occurrence!;
        Assert.Equal(guard ? -1 : factory.Villain, occurrence.Target);
        Assert.Equal(guard, occurrence.TargetFacts is null);
        Assert.Equal(guard ? 1 : 2, pending.Prompt!.Affordances.Count);
        int hand = factory.Game.State.Seats[0].Hand.Cards.Count;
        answer = Decision.Take(pending.Prompt.Affordances[0].Id);
        control.Resolve(answer);
        pending = Resolve(host, opened, pending, answer, "after-attack");
        Equivalent(control, factory.Game);
        Assert.Equal(hand + 1, factory.Game.State.Seats[0].Hand.Cards.Count);
        if (!guard)
        {
            control.Resolve(Decision.Decline);
            pending = Resolve(host, opened, pending, Decision.Decline, "decline-target-observer");
        }
        Assert.True(factory.Game.IsRootPrompt);
        Assert.Null(factory.Game.State.Activation);
        Assert.False(factory.Game.State.IsOver);
        Assert.Equal(guard ? 2 : 4, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, Assert.Single(pending.Events.OfType<BoostResolved>()).Icons);
        Assert.Single(pending.Events.OfType<AttackCompleted>());
        Equivalent(control, factory.Game);
    }

    private static (EngineHost Host, AttacrobaticsReplayReviewFactory Factory, MemorySessionStore Store, EngineResponse Pending)
        Restart(bool guard, EngineResponse opened, EngineResponse pending, MemorySessionStore store, Game control)
    {
        var prefix = Assert.Single(store.Load());
        string before = SessionSaveJson.Write(prefix.Save);
        var detached = StoredSessionJson.Read(StoredSessionJson.Write(prefix));
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(detached));
        var factory = new AttacrobaticsReplayReviewFactory(guard);
        var host = new EngineHost(factory, store: restoredStore);
        var restored = Exchange(host, EngineRequest.SyncGame("restart", Label, opened.Capability!));
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(before, SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(pending.Prompt!)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        return (host, factory, restoredStore, restored);
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
