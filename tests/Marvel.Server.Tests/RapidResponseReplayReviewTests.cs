using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RapidResponseReplayReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:enters-play")]
    [Rule("rr:response.3")]
    public void DefeatEntryAndCompletionRebuildAcrossStrictJsonAndFreshHosts(bool suitFirst)
    {
        // "Any time when a card transitions from an out-of-play area into play";
        // responses to one effect's triggering conditions "can be resolved in any order".
        var factory = new RapidResponseReplayReviewFactory();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "rapid-review",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7301)));
        Assert.Null(opened.Error);
        var current = Resolve(host, opened, opened, Take(opened, factory.Action));
        var uninterrupted = factory.Game;
        var defeat = Assert.Single(uninterrupted.State.Agenda.Occurrence!.Defeats);
        Assert.Equal(factory.Ally, defeat.Card);
        Assert.Equal(0, defeat.Controller);
        Assert.Equal(CardKind.Ally, defeat.Kind);
        host = Restart(ref factory, ref store, uninterrupted);
        current = host.Exchange(EngineRequest.SyncGame("defeat-restart", "rapid-review", opened.Capability!));
        Assert.Null(current.Error);
        current = Both(uninterrupted, host, opened, current, store, factory, Take(current, factory.Rapid));
        Assert.Equal(0, factory.Game.State.Cards[factory.Ally].Damage);
        Assert.Equal(4, factory.Game.State.Cards[factory.Ally].Tokens["c_arrow"]);
        current = Both(uninterrupted, host, opened, current, store, factory, Take(current, factory.Ally));
        Assert.DoesNotContain(factory.Game.State.Agenda.Outstanding, step => step.Operation is PreparationAbilityResolvedOperation);
        host = Restart(ref factory, ref store, uninterrupted);
        current = host.Exchange(EngineRequest.SyncGame("entry-restart", "rapid-review", opened.Capability!));
        Assert.Null(current.Error);
        // Synthetic status choice leaves Black Widow exhausted for printed Synth-Suit.
        current = Both(uninterrupted, host, opened, current, store, factory, Decision.Take(current.Prompt!.Affordances[0].Id));
        Assert.Equal(1, factory.Game.State.Cards[factory.Ally].Damage);
        var receipt = factory.Game.State.Agenda.Occurrence!.ActorFacts!.PreparationAbility;
        Assert.Equal("08031", Assert.IsType<ResolvedPreparationAbility>(receipt).Face);
        host = Restart(ref factory, ref store, uninterrupted);
        current = host.Exchange(EngineRequest.SyncGame("completion-restart", "rapid-review", opened.Capability!));
        Assert.Null(current.Error);
        int identity = factory.Game.State.Seats[0].IdentityCard.ObjectId;
        int first = suitFirst ? factory.Suit : identity;
        int second = suitFirst ? identity : factory.Suit;
        current = Both(uninterrupted, host, opened, current, store, factory, Take(current, first));
        if (!suitFirst)
            current = Both(uninterrupted, host, opened, current, store, factory,
                Decision.Take(factory.Game.State.TheCardIn(DeckType.VillainArea)!.ObjectId));
        current = Both(uninterrupted, host, opened, current, store, factory, Take(current, second));
        if (suitFirst)
            Both(uninterrupted, host, opened, current, store, factory,
                Decision.Take(factory.Game.State.TheCardIn(DeckType.VillainArea)!.ObjectId));
        Assert.True(factory.Game.IsRootPrompt);
        Assert.True(factory.Game.State.Seats[0].IdentityCard.Ready);
        Assert.False(factory.Game.State.Cards[factory.Suit].Ready);
        Assert.Equal(1, factory.Game.State.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, factory.Game.State.Cards[factory.Rapid].Area.Type);
    }

    private static EngineHost Restart(ref RapidResponseReplayReviewFactory factory, ref MemorySessionStore store, Game expected)
    {
        var saved = Assert.Single(store.Load());
        string json = SessionSaveJson.Write(saved.Save);
        // Occurrence facts are replay-derived, not hidden mutable save authority.
        Assert.DoesNotContain("\"defeats\"", json);
        var unknown = JsonNode.Parse(json)!;
        unknown["defeats"] = new JsonArray();
        Assert.Throws<SessionSaveException>(() => SessionSaveJson.Read(unknown.ToJsonString()));
        var restored = new MemorySessionStore();
        Assert.Null(restored.Commit(StoredSessionJson.Read(StoredSessionJson.Write(saved))));
        store = restored;
        factory = new RapidResponseReplayReviewFactory();
        var host = new EngineHost(factory, store: store);
        Assert.NotSame(expected.State, factory.Game.State);
        Equivalent(expected, factory.Game);
        Assert.Equal(json, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
        return host;
    }

    private static EngineResponse Both(Game expected, EngineHost host, EngineResponse opened,
        EngineResponse current, MemorySessionStore store, RapidResponseReplayReviewFactory factory, Decision answer)
    {
        var result = expected.Resolve(answer);
        var actual = Resolve(host, opened, current, answer);
        var persisted = Assert.Single(store.Load()).Save.Units[^1].Decisions[^1];
        Assert.Equal(result.Events.Select(JournalJson.Event).Select(item => item.GetRawText()),
            persisted.Events.Select(item => item.GetRawText()));
        Equivalent(expected, factory.Game);
        return actual;
    }

    private static Decision Take(EngineResponse response, int card) =>
        Decision.Take(Assert.Single(response.Prompt!.Affordances, offer => offer.AnchorId == card).Id);

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineJson.Snapshot(EngineRequest.ResolveGame("answer-" + current.Revision,
            "rapid-review", opened.Capability!, answer, current.Revision)));
        Assert.Null(result.Error);
        return EngineJson.Snapshot(result);
    }

    private static void Equivalent(Game expected, Game actual)
    {
        Assert.Equal(expected.State.Digest().Canonical(), actual.State.Digest().Canonical());
        Assert.Equal(expected.State.Random.Generator.GetState().Words, actual.State.Random.Generator.GetState().Words);
        Assert.Equal(expected.State.Random.Generator.GetState().Index, actual.State.Random.Generator.GetState().Index);
        Assert.Equal(expected.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(expected.Pending!)), JsonSerializer.Serialize(PromptRecord.From(actual.Pending!)));
        Assert.Equal(JsonSerializer.Serialize(expected.State.Agenda.Occurrence?.Defeats), JsonSerializer.Serialize(actual.State.Agenda.Occurrence?.Defeats));
        Assert.Equal(JsonSerializer.Serialize(expected.State.Agenda.Occurrence?.ActorFacts?.PreparationAbility),
            JsonSerializer.Serialize(actual.State.Agenda.Occurrence?.ActorFacts?.PreparationAbility));
        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Round, actual.Round);
    }
}
