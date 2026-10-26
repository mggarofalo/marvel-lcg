using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CounterattackReplayReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:villain-defeat.3.1")]
    public void ExactAttackReceiptAndDiscardedPreparationReconstructAcrossFreshHosts(bool promote, bool decline)
    {
        // "Forced abilities (such as the retaliate keyword)" precede optional responses;
        // a same-title stage "is considered the same character ... for purposes of card abilities".
        var factory = new CounterattackReplayReviewFactory(promote);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        const string label = "counterattack-review";
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 7301);
        var opened = host.Exchange(EngineRequest.OpenGame("open", label, specification));
        Assert.Null(opened.Error);
        var action = Assert.Single(opened.Prompt!.Affordances, offer => offer.AnchorId == factory.Action);
        var pending = host.Exchange(EngineRequest.ResolveGame("attack", label, opened.Capability!,
            Decision.Take(action.Id), opened.Revision));
        Assert.Null(pending.Error);
        var attackEvents = new List<GameEvent>(pending.Events!);
        for (int optional = 0; optional < 20
            && !pending.Prompt!.Affordances.Any(offer => offer.AnchorId == factory.Counter); optional++)
        {
            Assert.True(pending.Prompt!.Cancellable);
            pending = host.Exchange(EngineRequest.ResolveGame("optional-" + optional, label,
                opened.Capability!, Decision.Decline, pending.Revision));
            Assert.Null(pending.Error);
            attackEvents.AddRange(pending.Events!);
        }
        Assert.Single(attackEvents.OfType<AttackCompleted>());
        Assert.Equal(1, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(promote ? DeckType.RemovedArea : DeckType.VillainArea,
            factory.Game.State.Cards[factory.Enemy].Area.Type);
        Assert.Contains(pending.Prompt!.Affordances, offer => offer.AnchorId == factory.Counter);
        var original = factory.Game;
        string stored = RoundTrip(store);
        var fresh = new CounterattackReplayReviewFactory(promote);
        host = new EngineHost(fresh, store: store);
        pending = host.Exchange(EngineRequest.SyncGame("restart-attack", label, opened.Capability!));
        Assert.Null(pending.Error);
        Equivalent(original, fresh.Game);
        Assert.NotSame(original.State, fresh.Game.State);
        Assert.Equal(stored, StoredSessionJson.Write(Assert.Single(store.Load())));
        var receipt = Assert.Single(fresh.Game.State.Agenda.Occurrence!.ReceivedAttackDamage);
        Assert.Equal(fresh.Enemy, receipt.Attacker.Card);
        Assert.Equal(fresh.Game.State.Seats[0].IdentityCard.ObjectId, receipt.Recipient.Card);
        Assert.Equal(0, receipt.Recipient.Controller);
        Assert.Equal(1, receipt.Amount);
        var counter = Assert.Single(pending.Prompt!.Affordances, offer => offer.AnchorId == fresh.Counter);
        var answer = decline ? Decision.Decline : Decision.Take(counter.Id);
        var expected = original.Resolve(answer);
        pending = host.Exchange(EngineRequest.ResolveGame("response", label, opened.Capability!, answer, pending.Revision));
        Assert.Null(pending.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), pending.Events!.Select(EventJson.Write));
        Equivalent(original, fresh.Game);
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile,
            fresh.Game.State.Cards[fresh.Counter].Area.Type);
        Assert.Equal(decline ? 0 : 1, fresh.Game.State.Cards[promote ? fresh.Successor : fresh.Enemy].Damage - (promote ? 0 : 1));
        if (decline) Assert.True(fresh.Game.IsRootPrompt);
        else CompletePreparationAfterRestart(fresh, store, opened.Capability!, promote);
    }

    private static void CompletePreparationAfterRestart(CounterattackReplayReviewFactory previous,
        MemorySessionStore store, string capability, bool promote)
    {
        var completion = Assert.IsType<ResolvedPreparationAbility>(previous.Game.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08030", completion.Face);
        Assert.Equal(0, completion.Controller);
        var original = previous.Game;
        string stored = RoundTrip(store);
        var fresh = new CounterattackReplayReviewFactory(promote);
        var host = new EngineHost(fresh, store: store);
        var pending = host.Exchange(EngineRequest.SyncGame("restart-preparation", "counterattack-review", capability));
        Assert.Null(pending.Error);
        Equivalent(original, fresh.Game);
        Assert.Equal(stored, StoredSessionJson.Write(Assert.Single(store.Load())));
        var suit = Assert.Single(pending.Prompt!.Affordances, offer => offer.AnchorId == fresh.Suit);
        var expected = original.Resolve(Decision.Take(suit.Id));
        var result = host.Exchange(EngineRequest.ResolveGame("suit", "counterattack-review", capability,
            Decision.Take(suit.Id), pending.Revision));
        Assert.Null(result.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), result.Events!.Select(EventJson.Write));
        Equivalent(original, fresh.Game);
        Assert.True(fresh.Game.IsRootPrompt);
        Assert.True(fresh.Game.State.Seats[0].IdentityCard.Ready);
        Assert.False(fresh.Game.State.Cards[fresh.Suit].Ready);
        Assert.False(fresh.Game.State.IsOver);
        RoundTrip(store);
    }

    private static string RoundTrip(MemorySessionStore store)
    {
        var saved = Assert.Single(store.Load());
        string session = SessionSaveJson.Write(saved.Save);
        Assert.Equal(session, SessionSaveJson.Write(SessionSaveJson.Read(session)));
        string stored = StoredSessionJson.Write(saved);
        Assert.Equal(stored, StoredSessionJson.Write(StoredSessionJson.Read(stored)));
        Assert.DoesNotContain("received_attack_damage", session, StringComparison.Ordinal);
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(session.Insert(1, "\"unexpected_receipt\":1,")));
        Assert.Throws<SessionSaveException>(() => StoredSessionJson.Read(stored.Insert(1, "\"unexpected_receipt\":1,")));
        Assert.Null(store.Commit(StoredSessionJson.Read(stored)));
        return stored;
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
    }
}
