using Marvel.Content;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PowerOfAspectTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("01055", "01054")]
    [InlineData("01062", "01060")]
    [InlineData("01072", "01069")]
    [InlineData("01079", "01080")]
    public void PowerOfCardDoublesOnlyForItsAspect(string sourceId, string matchingId)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        var source = world.CreateCard(sourceId, seat.Hand);
        var matching = world.CreateCard(matchingId, seat.Hand);
        var basic = world.CreateCard("01087", seat.Hand);
        var abilities = AuthoredCards.Runner();
        world.Abilities = abilities;

        Assert.Equal("GG", abilities.ResourcesGeneratedBy(world, source, matching));
        Assert.Equal("G", abilities.ResourcesGeneratedBy(world, source, basic));

        var advertised = CardPlay.Generators(world, Cards, seat, matching)
            .Single(option => option.Effect == source.ObjectId);
        Assert.Equal("GG", advertised.Generates);
    }
}
