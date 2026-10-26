using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicBlackWidowRestartTests : EngineHostTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationPlayResponseSurvivesStrictSaveAndFreshHost(bool draw)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        // A naturally dealt printed starter supplies the Preparation.
        int seed = Enumerable.Range(1, 20).First(value => factory.Create(
            new GameSpecification("rhino", ["black_widow"], [], (uint)value)).Game.State.Seats[0]
            .Hand.Cards.Any(card => card.FaceId == "08024"));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "widow-public",
            new GameSpecification("rhino", ["black_widow"], [], (uint)seed)));
        Assert.Null(opened.Error);
        var current = Resolve(host, opened, opened, TakeOnly(opened));
        int preparation = Hand(current, 0).First(card => card.Face?.Id == "08024").Id!.Value;
        int identity = current.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Face?.Id == "08001b").Id!.Value;
        current = Resolve(host, opened, current, PayPreparation(current.Prompt!, preparation));

        // 08001b: "After you play a Preparation card, draw 1 card."
        Assert.DoesNotContain(Hand(current, 0), card => card.Id == preparation);
        var response = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == identity);
        Assert.True(current.Prompt.Cancellable);
        int beforeDraw = Hand(current, 0).Count;
        var copied = StoredSessionJson.Read(StoredSessionJson.Write(Assert.Single(store.Load())));
        var restoredStore = new MemorySessionStore();
        Assert.Null(restoredStore.Commit(copied));
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: restoredStore);
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "widow-public", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(current.Prompt)),
            JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)));
        Assert.Equal(SessionSaveJson.Write(copied.Save),
            SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));

        // A restored response remains bound to its exact revision and offer.
        // Rejected answers must leave the durable continuation available.
        foreach (var invalid in new[]
        {
            EngineRequest.ResolveGame("stale", "widow-public", opened.Capability!,
                Decision.Take(response.Id), restored.Revision - 1),
            EngineRequest.ResolveGame("unknown-offer", "widow-public", opened.Capability!,
                Decision.Take(int.MaxValue), restored.Revision),
        })
        {
            Assert.NotNull(restarted.Exchange(invalid).Error);
            Assert.Equal(SessionSaveJson.Write(copied.Save),
                SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
            var unchanged = restarted.Exchange(EngineRequest.SyncGame("unchanged", "widow-public", opened.Capability!));
            Assert.Null(unchanged.Error);
            Assert.Equal(restored.Revision, unchanged.Revision);
            Assert.Equal(JsonSerializer.Serialize(PromptRecord.From(restored.Prompt!)),
                JsonSerializer.Serialize(PromptRecord.From(unchanged.Prompt!)));
        }

        var answer = draw ? Decision.Take(response.Id) : Decision.Decline;
        var expected = Resolve(host, opened, current, answer);
        var actual = Resolve(restarted, opened, restored, answer);
        Assert.Equal(beforeDraw + (draw ? 1 : 0), Hand(actual, 0).Count);
        Assert.Equal(Hand(expected, 0).Select(card => card.Id), Hand(actual, 0).Select(card => card.Id));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
        Assert.Equal(SessionSaveJson.Write(Assert.Single(store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(restoredStore.Load()).Save));
        Assert.NotEmpty(actual.Prompt!.Affordances);
    }

    private static Decision PayPreparation(Prompt prompt, int preparation)
    {
        var action = Assert.Single(prompt.Affordances, offer => offer.AnchorId == preparation);
        var cost = Assert.Single(action.CostOptions);
        for (int count = 0; count <= cost.Generators.Count; count++)
        {
            int[] paying = cost.Generators.Take(count).Select(generator => generator.Effect).ToArray();
            var allocation = ResourcePayment.Allocate(cost, paying);
            if (allocation is not null && ResourcePayment.Allows(cost, paying, null, allocation))
                return Decision.Take(action.Id, action.Targets is null ? [] : [action.Targets.Legal[0]], paying,
                    new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The naturally dealt Preparation requires legal payment");
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current, Decision answer)
    {
        var result = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision,
            "widow-public", opened.Capability!, answer, current.Revision));
        Assert.Null(result.Error);
        return result;
    }
}
