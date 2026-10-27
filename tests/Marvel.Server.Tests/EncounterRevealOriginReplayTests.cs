using System.Text.Json;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EncounterRevealOriginReplayTests
{
    [Fact]
    [Rule("ruling:7075a85e02f2203e")]
    [Rule("rr:cancel.4")]
    public void HostedNestedDirectRevealRestoresItsOwnOriginAndReturnsToDealtParent()
    {
        // First-dealt cards qualify, including player-deck cards. Direct reveals
        // from separate zones do not. A canceled card is "still regarded as revealed".
        var factory = new EncounterRevealOriginReplayFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 828);
        const string label = "reveal-origin-review";
        var opened = host.Exchange(EngineRequest.OpenGame("open", label, specification));
        Assert.Null(opened.Error);
        Assert.Contains(opened.Prompt!.Affordances, a => a.AnchorId == factory.Ward);
        var outerReceipt = Assert.IsType<EncounterRevealOrigin>(factory.Game.State.Agenda.Occurrence!.RevealOrigin);
        Assert.True(outerReceipt.Dealt);
        Assert.Equal(factory.Outer, outerReceipt.Card);
        Assert.Equal(-1, factory.Game.State.Cards[factory.Outer].Owner);
        var nested = Decision.Take(Assert.Single(opened.Prompt.Affordances, a => a.AnchorId == factory.NestedReveal).Id);
        var suspended = host.Exchange(EngineRequest.ResolveGame("nest", label, opened.Capability!, nested, opened.Revision));
        Assert.Null(suspended.Error);
        Game control = factory.Game;
        Assert.Equal(factory.Inner, control.State.Agenda.Occurrence!.Subject);
        var innerReceipt = Assert.IsType<EncounterRevealOrigin>(control.State.Agenda.Occurrence.RevealOrigin);
        Assert.Equal(factory.Inner, innerReceipt.Card);
        Assert.Equal(nameof(DeckType.EncounterDiscardPile), innerReceipt.From.Zone);
        Assert.False(innerReceipt.FromEncounterDeck);
        Assert.DoesNotContain(suspended.Prompt!.Affordances, a => a.AnchorId == factory.Ward);
        Assert.Equal(DeckType.HandsArea, control.State.Cards[factory.Payment].Area.Type);

        string bytes = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        Game replay = SessionReplay.Verify(SessionSaveJson.Read(bytes), factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(control, replay);
        host = new EngineHost(factory, store: store);
        var resumed = host.Exchange(EngineRequest.SyncGame("restart", label, opened.Capability!));
        Assert.Null(resumed.Error);
        Assert.NotSame(control.State, factory.Game.State);
        Equivalent(control, factory.Game);
        Assert.Equal(bytes, SessionSaveJson.Write(Assert.Single(store.Load()).Save));

        var expected = control.Resolve(Decision.Decline);
        Assert.Equal(expected.Events.Select(EventJson.Write), replay.Resolve(Decision.Decline).Events.Select(EventJson.Write));
        var parent = host.Exchange(EngineRequest.ResolveGame("inner-decline", label, opened.Capability!, Decision.Decline, resumed.Revision));
        Assert.Null(parent.Error);
        Equivalent(control, factory.Game);
        Assert.Equal(outerReceipt, control.State.Agenda.Occurrence!.RevealOrigin);
        Assert.Equal(factory.Outer, control.State.Agenda.Occurrence.Subject);
        Assert.Contains(parent.Prompt!.Affordances, a => a.AnchorId == factory.Ward);
        Assert.True(Statuses.Has(control.State, control.State.TheCardIn(DeckType.VillainArea)!, Statuses.Tough));

        var draft = new DecisionComposer(control.Pending!);
        draft.SelectAffordance(Assert.Single(control.Pending!.Affordances, a => a.AnchorId == factory.Ward).Id);
        draft.ToggleResource(factory.Payment);
        Assert.True(draft.TryBuild(out Decision? cancel, out string? error), error);
        expected = control.Resolve(cancel!);
        Assert.Equal(expected.Events.Select(EventJson.Write), replay.Resolve(cancel!).Events.Select(EventJson.Write));
        var completed = host.Exchange(EngineRequest.ResolveGame("ward", label, opened.Capability!, cancel!, parent.Revision));
        Assert.Null(completed.Error);
        Equivalent(control, factory.Game);
        Equivalent(control, replay);
        Assert.True(control.IsRootPrompt);
        Assert.Empty(control.State.Agenda.Outstanding);
        Assert.Empty(control.State.AreaOf(DeckType.RevealingArea).Cards);
        Assert.Empty(control.State.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, control.State.Cards[factory.Outer].Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, control.State.Cards[factory.Inner].Area.Type);
        Assert.Equal(DeckType.DiscardPile, control.State.Cards[factory.Ward].Area.Type);
        Assert.Equal(DeckType.DiscardPile, control.State.Cards[factory.Payment].Area.Type);
        Assert.False(control.State.IsOver);
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.State.Agenda.Occurrence?.RevealOrigin, actual.State.Agenda.Occurrence?.RevealOrigin);
        Assert.Equal(expected.State.Agenda.Outstanding.Select(s => (s.What, s.Subject, Origin: (s.Operation as RevealStageOperation)?.Origin)),
            actual.State.Agenda.Outstanding.Select(s => (s.What, s.Subject, Origin: (s.Operation as RevealStageOperation)?.Origin)));
        Assert.Equal(expected.Phase, actual.Phase);
    }
}
