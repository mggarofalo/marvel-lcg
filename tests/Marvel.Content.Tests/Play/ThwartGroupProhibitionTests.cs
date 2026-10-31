using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards. Baron Zemo (03028): "you cannot thwart".
public sealed class ThwartGroupProhibitionTests : CoreCardTestBase
{
    private const string Zemo = "03028";

    [Theory]
    [InlineData("13003", "13001c", false)]
    [InlineData("13003", "13001c", true)]
    [InlineData("13003", "13001a", false)]
    [InlineData("13003", "13001a", true)]
    [InlineData("04038", "13001c", false)]
    [InlineData("04038", "13001c", true)]
    [Rule("rr:cannot")]
    public void AHeroWhoCannotThwartIsNotOfferedAThwartAction(string face, string form, bool prohibited)
    {
        // rr:cannot: "The word “cannot” is absolute and cannot be countermanded by other abilities or effects."
        // Giant Help (13003) and Inconspicuous (04038) are each a "Hero Action (thwart)".
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board(form);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        WaspBoards.Side(world, "01107", 3);
        if (prohibited) WaspBoards.Engaged(world, Zemo);
        var card = world.CreateCard(face, world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        Assert.Equal(!prohibited, ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
        Assert.Equal(prohibited ? 0 : 1, runner.Actions(world, 0).Count(action => action.Card == card.ObjectId));
    }

    [Theory]
    [InlineData("01012", "13001c", false)]
    [InlineData("01012", "13001c", true)]
    [InlineData("01023", "13001b", false)]
    [InlineData("01023", "13001b", true)]
    [InlineData("01037", "13001c", false)]
    [InlineData("01037", "13001c", true)]
    [Rule("rr:cannot")]
    public void AnUnlabelledThwartGroupIsNotOfferedToAHeroWhoCannotThwart(string face, string form, bool prohibited)
    {
        // rr:cannot: "The word “cannot” is absolute and cannot be countermanded by other abilities or effects."
        // Crisis Interdiction (01012) thwarts different schemes, Legal Practice (01023) thwarts with
        // discarded cards, and an AERIAL hero's Mark V Helmet (01037) thwarts every scheme.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board(form);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        WaspBoards.Side(world, "01107", 3);
        if (prohibited) WaspBoards.Engaged(world, Zemo);
        var card = Source(world, face);
        Assert.Equal(!prohibited, ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
        Assert.Equal(prohibited ? 0 : 1, runner.Actions(world, 0).Count(action => action.Card == card.ObjectId));
    }

    private static Card Source(World world, string face)
    {
        var seat = world.Seats[0];
        world.CreateCard("01089", seat.Hand);
        world.CreateCard("01089", seat.Hand);
        if (face != "01037") return world.CreateCard(face, seat.Hand);
        var helmet = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
            host: seat.IdentityCard.ObjectId, cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: Traits.Granted + "AERIAL",
            Amount: 1, Card: helmet.ObjectId, Affects: seat.IdentityCard.ObjectId));
        return helmet;
    }

    [Fact]
    [Rule("rr:confuse-confused.1")]
    public void ConfusedGiantWaspDiscardsConfusedInsteadOfDividingGiantHelp()
    {
        // rr:confuse-confused.1: "Forced Interrupt: When this character would scheme or thwart,
        // remove each confused status card from it instead." Giant Help is a labelled thwart.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001c");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var side = WaspBoards.Side(world, "01107", 3);
        var wasp = world.Seats[0].IdentityCard;
        Statuses.Give(world, wasp, Statuses.Confused);
        var card = world.CreateCard("13003", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        Sequence.Finish(world, Cards, runner, []);
        Assert.False(Statuses.Has(world, wasp, Statuses.Confused));
        Assert.Equal(5, main.Tokens["k_threat"]);
        Assert.Equal(3, side.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }
}
