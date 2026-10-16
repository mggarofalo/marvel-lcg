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

public sealed class NestedInterruptReplayTests
{
    [Fact]
    public void SavedDefenseInsideOptionalInterruptRecreatesItsAuthorityAndFuture()
    {
        // Replay is an engine contract: opaque live execution permits are
        // recreated from decisions, never serialized or reused across worlds.
        var factory = new NestedInterruptReplayFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["black_panther"], [], Seed: 931);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "nested", specification));
        Assert.Null(opened.Error);
        EngineResponse current = ReachInterrupt(host, opened, factory.Interrupt);
        Assert.Equal(4, factory.Game.State.Seats[0].IdentityCard.Damage);
        var draft = new DecisionComposer(current.Prompt!);
        draft.SelectAffordance(Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == factory.Interrupt).Id);
        draft.ToggleResource(factory.Resource);
        Assert.True(draft.TryBuild(out Decision? accepted, out string? error), error);
        EngineResponse suspended = host.Exchange(EngineRequest.ResolveGame(
            "interrupt", "nested", opened.Capability!, accepted!, current.Revision));
        Assert.Null(suspended.Error);
        Assert.Equal(PublicDecisionKind.Defense, suspended.Prompt!.PublicKind);
        Game uninterrupted = factory.Game;
        Assert.Equal(DeckType.RevealingArea, uninterrupted.State.Cards[factory.Interrupt].Area.Type);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal("open", save.Units[^1].Status);
        Assert.NotEmpty(save.Units[^1].Decisions);
        string bytes = SessionSaveJson.Write(save);
        Game restored = SessionReplay.Verify(SessionSaveJson.Read(bytes), factory.Compatibility, _ =>
        {
            OpenedGame fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Assert.NotSame(uninterrupted.State, restored.State);
        AssertEquivalent(uninterrupted, restored);
        Assert.Equal(bytes, SessionSaveJson.Write(save));

        for (int answer = 0; answer < 12 && uninterrupted.Phase != GamePhase.PlayerTurn; answer++)
        {
            Assert.True(uninterrupted.Pending!.Cancellable);
            Resolution expected = uninterrupted.Resolve(Decision.Decline);
            Resolution actual = restored.Resolve(Decision.Decline);
            Assert.Equal(expected.Events.Select(EventJson.Write), actual.Events.Select(EventJson.Write));
            Assert.Equal(JsonSerializer.Serialize(expected.Information), JsonSerializer.Serialize(actual.Information));
            AssertEquivalent(uninterrupted, restored);
        }
        Assert.Equal(GamePhase.PlayerTurn, restored.Phase);
        Assert.Equal(7, restored.State.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, restored.State.Cards[factory.Interrupt].Area.Type);
        Assert.Equal(1, restored.State.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    private static EngineResponse ReachInterrupt(EngineHost host, EngineResponse opened, int card)
    {
        EngineResponse current = opened;
        for (int answer = 0; answer < 30; answer++)
        {
            Prompt prompt = Assert.IsType<Prompt>(current.Prompt);
            if (prompt.PublicKind == PublicDecisionKind.Interrupt
                && prompt.Affordances.Any(offer => offer.AnchorId == card)) return current;
            Decision decision = prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            current = host.Exchange(EngineRequest.ResolveGame(
                $"prelude-{answer}", "nested", opened.Capability!, decision, current.Revision));
            Assert.Null(current.Error);
        }
        throw new InvalidOperationException("The villain phase did not offer Get Behind Me!");
    }

    private static void AssertEquivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)),
            JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Round, actual.Round);
        Assert.Equal(expected.State.Result, actual.State.Result);
    }
}
