using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicThorEngagementRestartTests : EngineHostTestBase
{
    [Fact]
    public void StarterDefenderEngagementResponseSurvivesFreshFactoryAndHost()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        // Bounded deterministic selection uses only the natural published starter hand.
        int seed = Enumerable.Range(1, 20).First(value => factory.Create(
            new GameSpecification("rhino_expert", ["thor"], [], (uint)value)).Game.State.Seats[0]
            .Hand.Cards.Any(card => card.FaceId == "06003"));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "thor-public",
            new GameSpecification("rhino_expert", ["thor"], [], (uint)seed)));
        Assert.Null(opened.Error);
        var current = Resolve(host, opened, opened, TakeOnly(opened));
        current = Resolve(host, opened, current,
            Decision.Take(Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == "Change_Form").Id));
        int identity = current.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "06001a").Id!.Value;
        int defender = Hand(current, 0).First(card => card.Face?.Id == "06003").Id!.Value;
        current = Resolve(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == defender).Id));
        var response = Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Timing == Marvel.Rules.Timing.AbilityType.Response);
        Assert.Contains(current.World!.Areas, area => area.Zone == "EngagedEnemiesArea" && area.Cards.Count > 0);
        Assert.DoesNotContain(Hand(current, 0), card => card.Id == defender);
        int beforeDraw = Hand(current, 0).Count;
        var copied = StoredSessionJson.Read(StoredSessionJson.Write(Assert.Single(store.Load())));
        // Capture the durable RNG receipts, pending prompt and complete decision
        // trace before probing. The fresh host below is the unprobed control.
        string beforeQueries = StoredSessionJson.Write(copied);
        for (int index = 0; index < 3; index++)
        {
            var queried = host.Exchange(EngineRequest.SyncGame("probe-" + index, "thor-public", opened.Capability!));
            Assert.Null(queried.Error);
            Assert.Equal(current.Revision, queried.Revision);
            Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(current.Prompt)),
                JsonSerializer.Serialize(PromptRecord.From(queried.Prompt!)));
            Assert.Equal(beforeQueries, StoredSessionJson.Write(Assert.Single(store.Load())));
        }
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(copied));
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: restoredStore);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "thor-public", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(current.Prompt)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(SessionSaveJson.Write(copied.Save), SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        var answer = Decision.Take(response.Id);
        var expected = Resolve(host, opened, current, answer);
        var actual = Resolve(restarted, opened, restored, answer);
        Assert.Equal(beforeDraw + 2, Hand(actual, 0).Count);
        Assert.Equal(Hand(expected, 0).Select(card => card.Id), Hand(actual, 0).Select(card => card.Id));
        Assert.Equal(SessionSaveJson.Write(Assert.Single(store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        Assert.NotEmpty(actual.Prompt!.Affordances);
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision,
            "thor-public", opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }
}
