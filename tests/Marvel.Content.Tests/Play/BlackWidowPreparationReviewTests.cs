using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreparationReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:referential-ability.step.2")]
    [Rule("rr:resolve.5")]
    public void ActualSuitUsesNamedIdentityEvenAcrossControllersAndInactiveForms(int suitController)
    {
        // The highest matching reference is "Cards associated with the same identity".
        // Constant abilities "are always active while the card they are on is in play."
        // This synthetic title-conflict board isolates reference precedence; it is
        // not a legal deck/setup or a public Black Widow admission claim.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AuthoredCards.Runner();
        var world = new World(facts, 2, 1701) { Abilities = runner };
        var first = world.CreateSeat("Black Widow");
        var widow = first.IdentityCard = world.CreateCard("08001a,08001b", first.Hero);
        var second = world.CreateSeat("Spider-Man");
        var spiderMan = second.IdentityCard = world.CreateCard("01001a,01001b", second.Hero);
        var ally = world.CreateCard("01075", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var suitArea = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(suitController), cardOwner: suitController);
        var suit = world.CreateCard("08009", suitArea);

        Assert.Equal("Black Widow", facts.Title(ally.FaceId));
        Assert.Equal(widow.ObjectId, Assert.Single(runner.Constant(world, suit)).Affects);
        Assert.Equal(3, Defense(widow));
        Assert.Equal(3, Defense(spiderMan));
        Assert.Equal(0, Defense(ally));

        widow.TurnTo("08001b");
        Assert.Equal("Natasha Romanoff", facts.Title(widow.FaceId));
        Assert.Empty(runner.Constant(world, suit));
        Assert.Equal(0, Defense(widow));
        Assert.Equal(3, Defense(spiderMan));
        Assert.Equal(0, Defense(ally));

        widow.TurnTo("08001a");
        Assert.Equal(3, Defense(widow));
        World.MoveToTop(suit, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(suitController), cardOwner: suitController));
        Assert.Equal(2, Defense(widow));
        Assert.Equal(0, Defense(ally));
        World.MoveToTop(suit, suitArea);
        Assert.Equal(3, Defense(widow));

        long Defense(Card card) => StateFields.Modified(world, card, "defense", facts, 2);
    }
}
