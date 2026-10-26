using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>A naturally dealt public starter reaches Bruno's suspended retrieval.</summary>
public sealed class MsMarvelChoicePersistenceTests(ITestOutputHelper output) : EngineHostTestBase
{
    [Fact]
    public void BrunoRetrievalRestartsWithStoredCardAndMatchesUninterruptedContinuation()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        // A bounded opening probe examined seeds 1..20 in order; 6 is the first with Bruno.
        // Selection seeks the characteristic card, never a winning game or a reordered deck.
        const uint seed = 6;
        output.WriteLine($"Natural published starter opening containing Bruno: seed={seed}");
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "ms-bruno",
            new GameSpecification("rhino", ["ms_marvel"], null, seed)));
        Assert.Null(opened.Error);
        var current = Resolve(host, opened, opened, TakeOnly(opened), "keep");
        int bruno = Assert.Single(Hand(current, 0), card => card.Face?.Id == "05007").Id!.Value;
        int endurance = Assert.Single(Hand(current, 0), card => card.Face?.Id == "05023").Id!.Value;
        current = Resolve(host, opened, current, PayOne(current.Prompt!, endurance, bruno), "play-endurance");
        current = Resolve(host, opened, current, PayOne(current.Prompt!, bruno), "play-bruno");
        var storage = current.Prompt!.Affordances.First(offer => offer.AnchorId == bruno);
        current = Resolve(host, opened, current, Decision.Take(storage.Id), "store-action");
        int held = Hand(current, 0)[0].Id!.Value;
        current = Resolve(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == held).Id), "store-card");
        Assert.DoesNotContain(Hand(current, 0), card => card.Id == held);
        var stored = Assert.Single(Assert.Single(current.World!.Areas, area => area.Host == bruno && area.Zone == "BeneathCardsPile").Cards);
        Assert.Null(stored.Face);
        Assert.False(stored.FaceUp);
        Assert.False(Assert.Single(current.World.Areas.SelectMany(area => area.Cards), card => card.Id == bruno).Ready);

        current = AdvanceToReadyBruno(host, opened, current, bruno);
        // Printed 05007: "Exhaust Bruno Carrelli → add up to 3 cards attached here to your hand."
        var retrieve = current.Prompt!.Affordances.Last(offer => offer.AnchorId == bruno);
        current = Resolve(host, opened, current, Decision.Take(retrieve.Id), "retrieve-action");
        var group = Assert.Single(current.Prompt!.Affordances);
        Assert.Equal(0, group.Targets!.Min);
        Assert.Equal(1, group.Targets.Max);
        Assert.Equal([held], group.Targets.Legal);
        Assert.False(Assert.Single(current.World!.Areas.SelectMany(area => area.Cards), card => card.Id == bruno).Ready);
        string authority = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        var restartStore = new MemorySessionStore();
        restartStore.Commit(Assert.Single(store.Load()));
        var restarted = new EngineHost(factory, store: restartStore);
        var before = host.Exchange(EngineRequest.SyncGame("sync", "ms-bruno", opened.Capability!));
        var restored = restarted.Exchange(EngineRequest.SyncGame("sync", "ms-bruno", opened.Capability!));
        Assert.Null(restored.Error);
        Assert.Equal(EngineJson.Write(before), EngineJson.Write(restored));
        Assert.Equal(authority, SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        var rejected = restarted.Exchange(EngineRequest.ResolveGame("invalid", "ms-bruno", opened.Capability!,
            new Decision(group.Id, [bruno]), restored.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Equal(authority, SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        Assert.Equal(EngineJson.Write(restored), EngineJson.Write(restarted.Exchange(
            EngineRequest.SyncGame("sync", "ms-bruno", opened.Capability!))));
        var answerGroup = new Decision(group.Id, [held]);
        var expected = Resolve(host, opened, current, answerGroup, "retrieve-card");
        var actual = Resolve(restarted, opened, restored, answerGroup, "retrieve-card");
        Assert.Equal(EngineJson.Write(expected), EngineJson.Write(actual));
        Assert.Equal(SessionSaveJson.Write(Assert.Single(store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        var returned = Assert.Single(Hand(actual, 0), card => card.Id == held);
        Assert.True(returned.FaceUp);
        Assert.Equal(-1, returned.Host);
        Assert.False(Assert.Single(actual.World!.Areas.SelectMany(area => area.Cards), card => card.Id == bruno).Ready);
    }

    private EngineResponse AdvanceToReadyBruno(EngineHost host, EngineResponse opened,
        EngineResponse current, int bruno)
    {
        for (int step = 0; step < 40 && !(current.Prompt?.Asking == Question.TurnOption
            && current.Prompt.Affordances.Any(offer => offer.AnchorId == bruno)); step++)
        {
            var prompt = Assert.IsType<Prompt>(current.Prompt);
            var answer = ProgressDecision(prompt, bruno);
            output.WriteLine($"advance-{step}: {prompt.Asking}, {prompt.Label}, answer={JsonSerializer.Serialize(answer)}");
            current = Resolve(host, opened, current, answer, $"advance-{step}");
        }
        return current;
    }

    private static Decision ProgressDecision(Prompt prompt, int bruno)
    {
        if (prompt.Affordances.FirstOrDefault(offer => offer.Verb == Game.EndPhaseVerb) is { } end)
            return Decision.Take(end.Id);
        if (prompt.Cancellable && prompt.Asking != Question.Defender) return Decision.Decline;
        var first = prompt.Affordances.First(offer => offer.IsLegal && offer.AnchorId != bruno);
        return new Decision(first.Id, first.Targets?.Legal.Take(first.Targets.Min).ToArray() ?? []);
    }

    private static Decision PayOne(Prompt prompt, int card, int preserve = -1)
    {
        var play = Assert.Single(prompt.Affordances, offer => offer.AnchorId == card);
        foreach (var cost in play.CostOptions)
        foreach (var source in cost.Generators.Where(source => source.Effect != preserve))
        {
            int[] payment = [source.Effect];
            var allocation = ResourcePayment.Allocate(cost, payment);
            if (allocation is not null && ResourcePayment.Allows(cost, payment, null, allocation))
                return Decision.Take(play.Id, play.Targets?.Legal.Take(play.Targets.Min).ToArray() ?? [], payment, new Dictionary<string, long>(), allocation);
        }
        throw new Xunit.Sdk.XunitException("The naturally drawn Bruno has no one-card payment");
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse opened, EngineResponse current,
        Decision decision, string id)
    {
        var response = host.Exchange(EngineRequest.ResolveGame(id, "ms-bruno", opened.Capability!, decision, current.Revision));
        Assert.True(response.Error is null, $"{id}: {response.Error}; prompt={JsonSerializer.Serialize(PromptRecord.From(current.Prompt!))}; answer={JsonSerializer.Serialize(decision)}");
        return response;
    }
}
