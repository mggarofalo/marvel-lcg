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

public sealed class OverkillPreventionReplayReviewTests
{
    private const string Label = "overkill-prevention-review";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    [Rule("rr:damage.3.2")]
    public void ExactOverkillPreventionCanBeTakenOrDeclinedAfterStrictJsonFreshHost(bool barrier, bool take)
    {
        // "If this attack defeats an ally" gives its excess to the controller's identity.
        // That excess "is considered damage from an attack"; prevention changes taken, not dealt.
        var factory = new OverkillPreventionReplayReviewFactory(barrier);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = Exchange(host, EngineRequest.OpenGame("open", Label,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        var pending = Resolve(host, opened, opened, Take(opened, factory.Action), "attack");
        Assert.Equal(Question.Defender, pending.Prompt!.Asking);
        pending = Resolve(host, opened, pending, Decision.Take(factory.Ally), "defend");
        Assert.Single(pending.Prompt!.Affordances, a => a.AnchorId == factory.Prevention);
        var occurrence = factory.Game.State.Agenda.Occurrence!;
        Assert.True(occurrence.DamageFromAttack);
        Assert.NotNull(occurrence.DirectDamage);
        long spill = occurrence.DirectDamage.Remaining;
        Assert.Equal(1, spill);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Ally].Area.Type);
        Assert.Equal(0, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(factory.Ally, factory.Game.State.Attack!.Defender);
        var control = factory.Game;
        (host, factory, store, pending) = Restart(barrier, opened, pending, store, control);

        var answer = take ? Take(pending, factory.Prevention) : Decision.Decline;
        var expected = control.Resolve(answer);
        var actual = Resolve(host, opened, pending, answer, "prevent");
        Assert.Equal(expected.Events.Select(EventJson.Write), actual.Events.Select(EventJson.Write));
        Equivalent(control, factory.Game);
        if (barrier && take)
        {
            Assert.Equal(Question.Element, actual.Prompt!.Asking);
            (host, factory, store, actual) = Restart(barrier, opened, actual, store, control);
            answer = Decision.Take(Assert.Single(actual.Prompt!.Affordances, a => a.Id == factory.Villain).Id);
            expected = control.Resolve(answer);
            actual = Resolve(host, opened, actual, answer, "reflect");
            Assert.Equal(expected.Events.Select(EventJson.Write), actual.Events.Select(EventJson.Write));
            Equivalent(control, factory.Game);
        }
        Assert.True(factory.Game.IsRootPrompt);
        Assert.False(factory.Game.State.IsOver);
        Assert.Equal(take ? 0 : spill, factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(barrier && take ? 1 : 0, factory.Game.State.Cards[factory.Villain].Damage);
        if (take) Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Prevention].Area.Type);
        else if (barrier) Assert.Equal(DeckType.UpgradesArea, factory.Game.State.Cards[factory.Prevention].Area.Type);
        else Assert.Same(factory.Game.State.Seats[0].Hand, factory.Game.State.Cards[factory.Prevention].Area);
        var completed = Assert.Single(actual.Events.OfType<AttackCompleted>());
        Assert.Equal(2, completed.DamageDealt);
        Assert.Equal(factory.Ally, completed.Target);
        Assert.Equal(factory.Ally, completed.Defender);
        _ = Restart(barrier, opened, actual, store, control);
    }

    private static (EngineHost Host, OverkillPreventionReplayReviewFactory Factory, MemorySessionStore Store, EngineResponse Pending)
        Restart(bool barrier, EngineResponse opened, EngineResponse pending, MemorySessionStore store, Game control)
    {
        string archive = StoredSessionJson.Write(Assert.Single(store.Load()));
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(StoredSessionJson.Read(archive)));
        var factory = new OverkillPreventionReplayReviewFactory(barrier);
        var host = new EngineHost(factory, store: restoredStore);
        var restored = Exchange(host, EngineRequest.SyncGame("restart", Label, opened.Capability!));
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(archive, StoredSessionJson.Write(Assert.Single(restoredStore.Load())));
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
        var response = EngineJson.ReadResponse(EngineJson.Write(host.Exchange(EngineJson.ReadRequest(EngineJson.Write(request)))));
        Assert.Null(response.Error);
        return response;
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
