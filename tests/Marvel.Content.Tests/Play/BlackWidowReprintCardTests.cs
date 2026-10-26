using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic actual-face coverage; no Black Widow setup or admission.</summary>
public sealed class BlackWidowReprintCardTests
{
    [Theory]
    [InlineData("08020", "01088", "YY")]
    [InlineData("08021", "01089", "BB")]
    [InlineData("08022", "01090", "RR")]
    [Rule("rr:max-maximum.2")]
    public void DoubleResourcesPayTwoAndShareTheirPrintedDeckMaximum(string face, string original, string icons)
    {
        // A per-deck maximum limits copies of a card by title, across printings.
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var team = world.CreateCard("08016", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        Assert.Equal(icons, Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, team), g => g.Effect == payment.ObjectId).Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], team, [payment.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, team.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(3, team.Tokens["c_snoop"]);
        Assert.Contains(face, AuthoredCards.Book.Authored);
        Assert.Empty(AuthoredCards.Book.On(face));
        var failure = Assert.Throws<ArgumentException>(() => Blueprints.From([
            new Creation(face, CreationSource.PlayerDeck, 0),
            new Creation(original, CreationSource.PlayerDeck, 0)], world.Facts));
        Assert.Contains("maximum is 1", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("08016", "GG", true)]
    [InlineData("01080", "G", false)]
    public void JusticePowerDoublesOnlyPrintedJusticeCardPayment(string target, string icons, bool enough)
    {
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(target, world.Seats[0].Hand);
        var power = world.CreateCard("08014", world.Seats[0].Hand);
        Assert.Equal(icons, Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, card), g => g.Effect == power.ObjectId).Generates);
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner), g => g.Effect == power.ObjectId).Generates);
        var extra = world.CreateCard("01093", world.Seats[0].Hand);
        if (enough)
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [power.ObjectId], []);
            Assert.Equal(DeckType.SupportsArea, card.Area.Type);
            Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        }
        else
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts,
                runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
        }
        Assert.Equal(2, world.Facts.PrintedValue("08014", "MaxPerDeck", 1));
        Assert.Equal(2, Blueprints.From([
            new Creation("08014", CreationSource.PlayerDeck, 0),
            new Creation("01062", CreationSource.PlayerDeck, 0)], world.Facts).Count);
        Assert.Throws<ArgumentException>(() => Blueprints.From([
            new Creation("08014", CreationSource.PlayerDeck, 0),
            new Creation("01062", CreationSource.PlayerDeck, 0),
            new Creation("08014", CreationSource.PlayerDeck, 0)], world.Facts));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:uses-x-type")]
    public void SurveillanceCommitsItsEffectEvenWhenItsLastCounterDiscardsIt(int counters)
    {
        // Uses: "When the last use counter is removed ... discard that card."
        Card team = null!;
        var (game, world) = Playing(board =>
        {
            team = InPlay(board, "08016");
            team.PlaceTokens("c_snoop", counters);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        });
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens["k_threat"];
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == team.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        if (counters > 1) Assert.False(team.Ready);
        Assert.Equal(counters == 1 ? DeckType.DiscardPile : DeckType.SupportsArea, team.Area.Type);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.AnchorId == scheme.ObjectId).Id));
        Assert.Equal(before - 1, scheme.Tokens["k_threat"]);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == team.ObjectId);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void EmptyOrExhaustedSurveillanceCannotPay(int counters, bool ready)
    {
        Card team = null!;
        var (game, _) = Playing(board =>
        {
            team = InPlay(board, "08016");
            team.PlaceTokens("c_snoop", counters);
            if (!ready) team.Exhaust();
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        });
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == team.ObjectId);
    }
}
