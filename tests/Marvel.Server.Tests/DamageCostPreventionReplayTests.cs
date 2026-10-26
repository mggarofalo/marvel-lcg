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

public sealed class DamageCostPreventionReplayTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.12")]
    public void PrintedPreventionRestartsAtCostInterruptAndNestedTargetWithoutRepaying(int target)
    {
        // Interrupts may initiate "just before that triggering condition becomes true".
        // Taking damage is unpaid "unless all of that damage was taken"; step 5
        // aborts "without paying any costs". FAQ 01027 confirms the printed pair.
        // Factory injection is nonpublic composition, not Ms. Marvel admission.
        var factory = new DamageCostPreventionReplayFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "damage-cost",
            new GameSpecification("rhino", ["she_hulk"], [], Seed: 7881)));
        Assert.Null(opened.Error);
        int held = factory.Game.State.Seats[0].Hand.Cards.Count;
        long damage = factory.Game.State.Seats[0].IdentityCard.Damage;
        var action = Assert.Single(opened.Prompt!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == factory.Rage);
        var current = Resolve(host, opened, opened, Decision.Take(action.Id));
        Assert.Equal(Question.Opportunity, current.Prompt!.Asking);
        Assert.Contains(current.Prompt.Affordances, a => a.AnchorId == factory.Barrier);
        var control = factory.Game;
        AssertUnpaid(control, factory.Rage, held, damage);
        host = Restart(factory, ref store, control);
        current = host.Exchange(EngineRequest.SyncGame("interrupt-sync", "damage-cost", opened.Capability!));
        Assert.Null(current.Error);
        AssertRejectedIsAtomic(host, store, factory, opened, current);

        var interrupt = target < 0 ? Decision.Decline : Decision.Take(Assert.Single(
            current.Prompt!.Affordances, a => a.AnchorId == factory.Barrier).Id);
        current = ResolveCompared(host, store, factory, opened, current, control, interrupt);
        if (target >= 0)
        {
            Assert.Equal(Question.Element, current.Prompt!.Asking);
            AssertUnpaid(factory.Game, factory.Rage, held, damage);
            Assert.Equal(new[] { factory.Villain, factory.Minion }.Order(),
                current.Prompt.Affordances.Select(a => a.Id).Order());
            host = Restart(factory, ref store, control);
            current = host.Exchange(EngineRequest.SyncGame("target-sync", "damage-cost", opened.Capability!));
            Assert.Null(current.Error);
            AssertRejectedIsAtomic(host, store, factory, opened, current);
            current = ResolveCompared(host, store, factory, opened, current, control,
                Decision.Take(target == 0 ? factory.Villain : factory.Minion));
        }

        Assert.Equal(Question.TurnOption, current.Prompt!.Asking);
        Assert.Equal(held + (target < 0 ? 1 : 0), factory.Game.State.Seats[0].Hand.Cards.Count);
        Assert.Equal(damage + (target < 0 ? 1 : 0), factory.Game.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(target >= 0, factory.Game.State.Cards[factory.Rage].Ready);
        Assert.Equal(target >= 0 ? DeckType.DiscardPile : DeckType.UpgradesArea,
            factory.Game.State.Cards[factory.Barrier].Area.Type);
        Assert.Equal(target == 0 ? 1 : 0, factory.Game.State.Cards[factory.Villain].Damage);
        Assert.Equal(target == 1 ? 1 : 0, factory.Game.State.Cards[factory.Minion].Damage);
        Assert.False(factory.Game.State.Agenda.IsBusy);
        Assert.Equal("complete", Assert.Single(store.Load()).Save.Units[^1].Status);
        // Completed units also reconstruct without a repeated draw or reflected damage.
        Restart(factory, ref store, control);
    }

    private static void AssertUnpaid(Game game, int rage, int held, long damage)
    {
        Assert.True(game.State.Cards[rage].Ready);
        Assert.Equal(held, game.State.Seats[0].Hand.Cards.Count);
        Assert.Equal(damage, game.State.Seats[0].IdentityCard.Damage);
    }

    private static EngineHost Restart(DamageCostPreventionReplayFactory factory,
        ref MemorySessionStore store, Game expected)
    {
        var saved = Assert.Single(store.Load());
        string json = StoredSessionJson.Write(saved);
        var copy = StoredSessionJson.Read(json);
        Assert.Equal(json, StoredSessionJson.Write(copy));
        var fresh = new MemorySessionStore();
        Assert.Null(fresh.Commit(copy));
        store = fresh;
        var host = new EngineHost(factory, store: store);
        Assert.Equal(json, StoredSessionJson.Write(Assert.Single(store.Load())));
        Assert.NotSame(expected.State, factory.Game.State);
        Assert.Equal(Snapshot(expected), Snapshot(factory.Game));
        return host;
    }

    private static void AssertRejectedIsAtomic(EngineHost host, MemorySessionStore store,
        DamageCostPreventionReplayFactory factory, EngineResponse opened, EngineResponse current)
    {
        string state = Snapshot(factory.Game);
        string save = StoredSessionJson.Write(Assert.Single(store.Load()));
        var invalid = host.Exchange(EngineRequest.ResolveGame("invalid-" + current.Revision,
            "damage-cost", opened.Capability!, Decision.Take(int.MaxValue), current.Revision));
        Assert.NotNull(invalid.Error);
        Assert.Equal(state, Snapshot(factory.Game));
        Assert.Equal(save, StoredSessionJson.Write(Assert.Single(store.Load())));
        var stale = host.Exchange(EngineRequest.ResolveGame("stale-" + current.Revision,
            "damage-cost", opened.Capability!, Decision.Take(current.Prompt!.Affordances[0].Id),
            current.Revision - 1));
        Assert.NotNull(stale.Error);
        Assert.Equal(state, Snapshot(factory.Game));
        Assert.Equal(save, StoredSessionJson.Write(Assert.Single(store.Load())));
    }

    private static EngineResponse ResolveCompared(EngineHost host, MemorySessionStore store,
        DamageCostPreventionReplayFactory factory, EngineResponse opened, EngineResponse current,
        Game control, Decision answer)
    {
        var expected = control.Resolve(answer);
        var result = Resolve(host, opened, current, answer);
        var recorded = Assert.Single(store.Load()).Save.Units[^1].Decisions[^1];
        Assert.Equal(expected.Events.Select(JournalJson.Event).Select(e => e.GetRawText()),
            recorded.Events.Select(e => e.GetRawText()));
        Assert.Equal(Snapshot(control), Snapshot(factory.Game));
        string save = StoredSessionJson.Write(Assert.Single(store.Load()));
        var duplicate = host.Exchange(EngineRequest.ResolveGame("duplicate-" + current.Revision,
            "damage-cost", opened.Capability!, answer, current.Revision));
        Assert.NotNull(duplicate.Error);
        Assert.Equal(save, StoredSessionJson.Write(Assert.Single(store.Load())));
        Assert.Equal(Snapshot(control), Snapshot(factory.Game));
        return result;
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened,
        EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision,
            "damage-cost", opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }

    private static string Snapshot(Game game) => JsonSerializer.Serialize(new
    {
        Digest = game.State.Digest().Canonical(),
        game.State.Random.Generator.GetState().Words,
        game.State.Random.Generator.GetState().Index,
        game.State.Random.Generator.WordsConsumed,
        Prompt = PromptRecord.From(game.Pending!),
        game.Phase, game.Round, game.State.Result,
        game.State.Agenda.Count, game.State.Agenda.IsBusy
    });
}
