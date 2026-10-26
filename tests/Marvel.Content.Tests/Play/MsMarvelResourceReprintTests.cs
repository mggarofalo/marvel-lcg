using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Ms. Marvel faces in prepared nonpublic card-execution scenes.</summary>
public sealed class MsMarvelResourceReprintTests
{
    [Theory]
    [InlineData("05019", "YY")]
    [InlineData("05020", "BB")]
    [InlineData("05021", "RR")]
    public void PrintedDoubleResourcesActuallyPayForReflexes(string face, string resources)
    {
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var upgrade = world.CreateCard("05024", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        var generator = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, upgrade),
            item => item.Effect == payment.ObjectId);
        Assert.Equal(resources, generator.Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], upgrade, [payment.ObjectId], []);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(3, upgrade.Tokens["c_energy"]);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData("01080", "GG", true)]
    [InlineData("01083", "G", false)]
    [InlineData("05003", "G", false)]
    public void ProtectionDoublingQualifiesTheActualCardAndChangesPayment(string face, string resources, bool enough)
    {
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var power = world.CreateCard("05016", world.Seats[0].Hand);
        var extra = world.CreateCard("01003", world.Seats[0].Hand);
        Assert.Equal(resources, Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card),
            item => item.Effect == power.ObjectId).Generates);
        // Same-cost Med Team / Mockingbird distinguish printed Protection from a generic multiplier.
        if (face == "05003") return;
        var before = world.Digest().Canonical();
        if (!enough)
        {
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
                world.Seats[0], card, [power.ObjectId, extra.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
        }
        else
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []);
            Assert.Equal(DeckType.SupportsArea, card.Area.Type);
            Assert.Equal(3, card.Tokens["c_medical"]);
            Assert.Equal(DeckType.DiscardPile, power.Area.Type);
            Assert.Equal(DeckType.DiscardPile, extra.Area.Type);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MansionDrawsOnlyForChosenPlayerAndExhausts(int recipient)
    {
        Card? mansion = null;
        var (game, world) = Playing(board => mansion = InPlay(board, "05022"),
            heroes: ["spider_man", "captain_marvel"]);
        int[] held = world.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        var top = world.Seats[recipient].Deck.Cards[^1];
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == mansion!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(mansion!.Ready);
        game.Resolve(Decision.Take(world.Seats[recipient].IdentityCard.ObjectId));
        Assert.Same(world.Seats[recipient].Hand, top.Area);
        for (int player = 0; player < 2; player++)
            Assert.Equal(held[player] + (player == recipient ? 1 : 0), world.Seats[player].Hand.Cards.Count);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == mansion.ObjectId);
    }
}
