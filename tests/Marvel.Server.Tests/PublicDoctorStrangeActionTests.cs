using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeActionTests : EngineHostTestBase
{
    [Fact]
    public void NaturallyShuffledWindsResolvesThroughThePublicHostAfterMulliganAndFormChange()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        // Bounded seeded fixture selection preserves the production shuffle and starter.
        uint seed = (uint)Enumerable.Range(1, 30).First(value =>
            factory.Create(new GameSpecification("rhino", ["doctor_strange"], [], (uint)value))
                .Game.State.Seats[0].InvocationDeck!.Deck.Cards[^1].FaceId == "09036");
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "strange-action",
            new GameSpecification("rhino", ["doctor_strange"], [], seed)));
        Assert.Null(opened.Error);
        var current = Answer(host, opened, opened, TakeOnly(opened));
        var invocation = Assert.Single(current.World!.InvocationDecks);
        var deck = current.World.Areas.Single(area => area.Id == invocation.Deck);
        Assert.Equal(5, deck.Cards.Count);
        var top = Assert.Single(deck.Cards, card => card.Face?.Id == "09036");
        Assert.Equal(4, deck.Cards.Count(card => card.Face is null));
        var change = Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == Game.ChangeForm);
        current = Answer(host, opened, current, Decision.Take(change.Id));
        int identity = current.World!.Areas.SelectMany(area => area.Cards).Single(card => card.Face?.Id == "09001a").Id!.Value;
        var mastery = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == identity && offer.Verb == Game.ActionVerb);
        Assert.Equal([top.Id!.Value], mastery.Targets!.Legal);
        Assert.Contains("Exhaust Doctor Strange", mastery.CostDescription);
        Assert.Equal("0", Assert.Single(mastery.CostOptions).Cost);
        int hand = Hand(current, 0).Count;
        current = Answer(host, opened, current, Decision.Take(mastery.Id, [top.Id.Value], []));
        Assert.Equal(hand + 3, Hand(current, 0).Count);
        Assert.Equal("09036", Assert.Single(current.World!.Areas.Single(area => area.Id == invocation.Discard).Cards).Face!.Id);
        Assert.Equal(4, current.World.Areas.Single(area => area.Id == invocation.Deck).Cards.Count);
        Assert.Single(current.World.Areas.Single(area => area.Id == invocation.Deck).Cards, card => card.Face is not null);
    }

    private static EngineResponse Answer(EngineHost host, EngineResponse opened, EngineResponse current, Decision decision)
    {
        var response = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision, opened.GameId,
            opened.Capability!, decision, current.Revision));
        Assert.Null(response.Error);
        return response;
    }
}
