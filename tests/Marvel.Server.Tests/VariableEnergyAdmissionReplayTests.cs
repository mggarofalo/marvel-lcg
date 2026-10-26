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

/// <summary>Synthetic damage admission through the supported durable factory seam.</summary>
public sealed class VariableEnergyAdmissionReplayTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void InsignificantEnergyRejectsBeforeDurablePaymentAndValidRetryRestarts()
    {
        // "Abort this process without paying any costs." Save/replay are repository choices.
        const string effect = """
            {"if":{"test":{"paidWithResource":"R"},"then":{
              "dealDamage":{"cards":{"query":"villain"},"amount":{"add":[{"result":"energy"},-1]}}},
              "else":{"dealDamage":{"cards":{"query":"villain"},"amount":0}}}}
            """;
        var factory = new VariableEventReplayReviewFactory(effect, "YYYR");
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var specification = new GameSpecification("rhino", ["spider_man"], [], Seed: 12345);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "energy-admission-review", specification));
        Assert.Null(opened.Error);
        var action = Assert.Single(opened.Prompt!.Affordances, offer => offer.AnchorId == factory.Source);
        var initial = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var before = factory.Game.State.Digest().Canonical();
        var random = factory.Game.State.Random.Generator.GetState();
        long words = factory.Game.State.Random.Generator.WordsConsumed;
        var rejected = host.Exchange(EngineRequest.ResolveGame("reject", "energy-admission-review", opened.Capability!,
            Payment(action.Id, factory.Generator, 1), opened.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Equal(initial, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(before, factory.Game.State.Digest().Canonical());
        Assert.Equal(random.Index, factory.Game.State.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, factory.Game.State.Random.Generator.GetState().Words);
        Assert.Equal(words, factory.Game.State.Random.Generator.WordsConsumed);
        var wrongBranch = host.Exchange(EngineRequest.ResolveGame("wrong-branch", "energy-admission-review", opened.Capability!,
            Payment(action.Id, factory.Generator, 2, "Y"), opened.Revision));
        Assert.NotNull(wrongBranch.Error);
        Assert.Equal(initial, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal(before, factory.Game.State.Digest().Canonical());
        var pending = host.Exchange(EngineRequest.ResolveGame("retry", "energy-admission-review", opened.Capability!,
            Payment(action.Id, factory.Generator, 2), opened.Revision));
        Assert.Null(pending.Error);
        Assert.Equal(Question.Opportunity, pending.Prompt!.Asking);
        var uninterrupted = factory.Game;
        Assert.Equal(DeckType.RevealingArea, uninterrupted.State.Cards[factory.Source].Area.Type);
        Assert.Equal(0, uninterrupted.State.TheCardIn(DeckType.VillainArea)!.Damage);
        var save = SessionSaveJson.Read(SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        Assert.Equal("open", save.Units[^1].Status);
        var accepted = Assert.Single(save.Units[^1].Decisions).Decision;
        Assert.Equal(2, accepted.Values["X"]);
        Assert.Equal(new[] { factory.Generator }, accepted.Resources);
        Assert.Equal(new ResourceAllocation[] { new(factory.Generator, 0, "R"), new(factory.Generator, 1, "YY") },
            accepted.Allocations);
        var replayed = SessionReplay.Verify(save, factory.Compatibility, _ =>
        {
            var fresh = factory.Create(specification);
            return new ReplayOpenedGame(fresh.Game, fresh.SetupEvents);
        });
        Equivalent(uninterrupted, replayed);
        var restarted = new EngineHost(factory, store: store);
        var restored = restarted.Exchange(EngineRequest.SyncGame("restart", "energy-admission-review", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.NotSame(uninterrupted.State, factory.Game.State);
        Equivalent(uninterrupted, factory.Game);
        var expected = uninterrupted.Resolve(Decision.Decline);
        var replayResult = replayed.Resolve(Decision.Decline);
        var completed = restarted.Exchange(EngineRequest.ResolveGame("finish", "energy-admission-review", opened.Capability!,
            Decision.Decline, restored.Revision));
        Assert.Null(completed.Error);
        Assert.Equal(expected.Events.Select(EventJson.Write), replayResult.Events.Select(EventJson.Write));
        Assert.Equal(expected.Events.Select(EventJson.Write), completed.Events!.Select(EventJson.Write));
        Equivalent(uninterrupted, factory.Game);
        Equivalent(uninterrupted, replayed);
        Assert.Equal(1, factory.Game.State.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Source].Area.Type);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Generator].Area.Type);
        Assert.False(factory.Game.State.Agenda.IsBusy);
    }

    private static Decision Payment(int action, int generator, int x, string printed = "R") => Decision.Take(action, [], [generator],
        new Dictionary<string, long> { ["X"] = x }, [new(generator, 0, printed), new(generator, 1, new string('Y', x))]);

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)),
            JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Round, actual.Round);
    }
}
