using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RecipientLegalGameTests(ITestOutputHelper output) : EngineHostTestBase
{
    [Theory]
    [InlineData("01091", 1)]
    [InlineData("01091", 2)]
    [InlineData("01092", 1)]
    [InlineData("01092", 2)]
    public void LegalSupportPlayThenOneRecipientAnswerCommitsOneRevision(string face, int players)
    {
        var (host, opened, seed) = OpeningWith(face, players);
        string capability = RequiredCapability(opened);
        EngineResponse current = opened;
        for (int seat = 0; seat < players; seat++)
            current = Resolve(host, current, capability, TakeOnly(current), $"keep-{seat}");
        CardDescriptor support = Hand(current, 0).Single(card => card.Face?.Id == face);
        current = Resolve(host, current, capability, PayCard(current.Prompt!, support.Id!.Value), "play");
        Affordance activation = Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == support.Id && offer.DirectRecipientCommit);
        Assert.Equal(players, activation.Targets!.Legal.Count);
        int recipient = activation.Targets.Legal[^1];
        int recipientSeat = current.World!.Areas.SelectMany(area => area.Cards)
            .Single(card => card.Id == recipient).Location!.Controller;
        int before = Hand(current, recipientSeat).Count;
        long revision = current.Revision;
        var composer = new DecisionComposer(current.Prompt);
        composer.SelectAffordance(activation.Id);
        composer.SelectTargets([recipient]);
        Assert.True(composer.TryBuild(out var answer, out var error), error);
        EngineResponse committed = Resolve(host, current, capability, answer!, "recipient");
        Assert.Equal(revision + 1, committed.Revision);
        Assert.Equal(PublicDecisionKind.PlayerAction, committed.Prompt!.PublicKind);
        Assert.False(committed.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Id == support.Id).Ready);
        Assert.Equal(before + (face == "01091" ? 1 : 0), Hand(committed, recipientSeat).Count);
        Assert.Contains(committed.Events, entry => entry is FieldSet { Field: "is_exhaust", To: 1 });
        EngineResponse repeated = host.Exchange(EngineRequest.ResolveGame("stale", "recipient-game", capability, answer!, revision));
        Assert.NotNull(repeated.Error);
        EngineResponse synced = host.Exchange(EngineRequest.SyncGame("sync", "recipient-game", capability));
        Assert.Equal(committed.Revision, synced.Revision);
        Assert.Equal(Hand(committed, recipientSeat).Count, Hand(synced, recipientSeat).Count);
        output.WriteLine($"RECIPIENT_LEGAL_SEED face={face} players={players} seed={seed}");
    }

    private static EngineResponse Resolve(EngineHost host, EngineResponse current, string capability,
        EngineDecision answer, string request)
    {
        var response = host.Exchange(EngineRequest.ResolveGame(request, "recipient-game", capability, answer, current.Revision));
        Assert.Null(response.Error);
        return response;
    }

    private static (EngineHost Host, EngineResponse Opened, uint Seed) OpeningWith(string face, int players)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        for (uint seed = 1; seed <= 64; seed++)
        {
            var host = new EngineHost(factory);
            var opened = host.Exchange(EngineRequest.OpenGame("open", "recipient-game",
                new GameSpecification("rhino", players == 1 ? ["spider_man"] : ["spider_man", "captain_marvel"], [], Seed: seed)));
            Assert.Null(opened.Error);
            if (Hand(opened, 0).Any(card => card.Face?.Id == face)) return (host, opened, seed);
        }
        throw new Xunit.Sdk.XunitException("No opening support found in the bounded deterministic seed set.");
    }
}
