using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Doctor Strange reprints in nonpublic prepared execution scenes.</summary>
public sealed class DoctorStrangeResourceReprintTests
{
    [Theory]
    [InlineData("09017", "01079")]
    [InlineData("09018", "01080")]
    [InlineData("09022", "01088")]
    [InlineData("09023", "01089")]
    [InlineData("09024", "01090")]
    [InlineData("09025", "01091")]
    public void ReprintsHaveTheSamePrintedRulesAndMetadataAsTheirCoreFaces(string face, string original)
    {
        using var catalog = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var cards = catalog.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        var reprint = Assert.Single(cards, card => card.GetProperty("card_id").GetString() == face);
        var core = Assert.Single(cards, card => card.GetProperty("card_id").GetString() == original);
        foreach (string field in new[] { "name", "type", "traits", "attributes", "text", "text_plain" })
            Assert.Equal(core.GetProperty(field).GetRawText(), reprint.GetProperty(field).GetRawText());
    }

    [Theory]
    [InlineData("09022", "01088", "YY")]
    [InlineData("09023", "01089", "BB")]
    [InlineData("09024", "01090", "RR")]
    [Rule("rr:max-maximum.2")]
    public void DoubleResourcesPayTwoAndShareTheirPrintedDeckMaximum(string face, string original, string icons)
    {
        // rr:max-maximum.2: "Max X per deck" restricts the number of copies in each player deck.
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var support = world.CreateCard("01026", world.Seats[0].Hand);
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        Assert.Equal(icons, Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, support), item => item.Effect == payment.ObjectId).Generates);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], support, [payment.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, support.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Contains(face, AuthoredCards.Book.Authored);
        Assert.Empty(AuthoredCards.Book.On(face));
        var failure = Assert.Throws<ArgumentException>(() => Blueprints.From([
            new Creation(face, CreationSource.PlayerDeck, 0),
            new Creation(original, CreationSource.PlayerDeck, 0)], world.Facts));
        Assert.Contains("maximum is 1", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("09018", "GG", true)]
    [InlineData("01083", "G", false)]
    public void ProtectionDoublingChangesOnlyProtectionCardPayment(string target, string icons, bool enough)
    {
        var (_, world) = Playing(_ => { });
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(target, world.Seats[0].Hand);
        var power = world.CreateCard("09017", world.Seats[0].Hand);
        var extra = world.CreateCard("01003", world.Seats[0].Hand);
        Assert.Equal(icons, Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, card), item => item.Effect == power.ObjectId).PossibleResources);
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner), item => item.Effect == power.ObjectId).Generates);
        var abilitySource = InPlay(world, "09018");
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, world.Facts,
            world.Seats[0], runner, abilitySource: abilitySource), item => item.Effect == power.ObjectId).Generates);
        if (enough)
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []);
            Assert.Equal(DeckType.SupportsArea, card.Area.Type);
            Assert.Equal(3, card.Tokens["c_medical"]);
            Assert.Equal(DeckType.DiscardPile, power.Area.Type);
            Assert.Equal(DeckType.DiscardPile, extra.Area.Type);
        }
        else
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts,
                runner, world.Seats[0], card, [power.ObjectId, extra.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
        }
        Assert.Equal(2, world.Facts.PrintedValue("09017", "MaxPerDeck", 1));
        Assert.Equal(2, Blueprints.From([
            new Creation("09017", CreationSource.PlayerDeck, 0),
            new Creation("01079", CreationSource.PlayerDeck, 0)], world.Facts).Count);
        Assert.Throws<ArgumentException>(() => Blueprints.From([
            new Creation("09017", CreationSource.PlayerDeck, 0),
            new Creation("01079", CreationSource.PlayerDeck, 0),
            new Creation("09017", CreationSource.PlayerDeck, 0)], world.Facts));
    }
}
