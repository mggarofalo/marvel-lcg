using Marvel.Content;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CountdownToOblivionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:cannot")]
    [Fact]
    public void ThreatCannotBeRemovedWhileStageThreeIsInPlay()
    {
        // "Threat cannot be removed from this scheme." The word "cannot" is
        // absolute, and the constant applies only while this face is in play.
        var world = new World(Cards, players: 1);
        world.CreateSeat("p0");
        var scheme = world.CreateCard("01139b", world.AreaOf(DeckType.MainSchemesArea));
        var abilities = AuthoredCards.Runner();

        Assert.False(abilities.CanRemoveThreat(world, scheme));

        World.MoveToTop(scheme, world.AreaOf(DeckType.MainSchemesDeck));

        Assert.True(abilities.CanRemoveThreat(world, scheme));
    }
}
