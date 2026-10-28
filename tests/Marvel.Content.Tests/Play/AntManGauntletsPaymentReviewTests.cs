using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic board using the production ability book and Game decision boundary.
public sealed class AntManGauntletsPaymentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("12001a", "Y", "confused")]
    [InlineData("12001c", "R", "stunned")]
    [Rule("rr:wild-resource.1")]
    public void WrongWildDeclarationIsRejectedBeforeCostsAndValidRetryWorks(string face, string icon, string status)
    {
        // Players "may specify which resource type" a wild resource is used as.
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("12010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("01055", seat.Hand);
        var second = world.CreateCard("01055", seat.Hand);
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [], [first.ObjectId, second.ObjectId],
            Allocations: [new(first.ObjectId, 0, "B"), new(second.ObjectId, 0, icon)])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Same(prompt, game.Pending);
        Assert.True(source.Ready);
        game.Resolve(new Decision(offer.Id, [], [first.ObjectId, second.ObjectId],
            Allocations: [new(first.ObjectId, 0, icon), new(second.ObjectId, 0, icon)]));
        var choice = game.Pending!;
        Assert.Contains(choice.Affordances, a => a.Id == villain.ObjectId);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.True(Statuses.Has(world, villain, status));
        Assert.False(source.Ready);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.True(game.IsRootPrompt);
    }
}
