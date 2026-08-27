using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreDamageProhibitionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:cannot")]
    [Fact]
    public void KillmongerRejectsOnlyBlackPantherUpgradeSources()
    {
        // "Cannot take damage from Black Panther upgrades" names both the
        // source's current kind and trait. A basic upgrade is not prohibited.
        var world = Board();
        var killmonger = world.CreateCard(
            "01157", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var pantherUpgrade = world.CreateCard(
            "01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var basicUpgrade = world.CreateCard(
            "01093", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.Equal(CardKind.Upgrade, Cards.Kind(pantherUpgrade.FaceId));
        Assert.Contains("BLACK_PANTHER", Traits.Of(world, pantherUpgrade, Cards));
        Assert.False(runner.CanTakeDamage(world, killmonger, pantherUpgrade));
        Assert.True(runner.CanTakeDamage(world, killmonger, basicUpgrade));

        Damage.Deal(world, Cards, pantherUpgrade, killmonger, 3, "test", "Damage", []);
        Assert.Equal(0, killmonger.Damage);

        Damage.Deal(world, Cards, basicUpgrade, killmonger, 2, "test", "Damage", []);
        Assert.Equal(2, killmonger.Damage);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:cannot")]
    [Fact]
    public void MadameHydrasProhibitionTracksLegionsOfHydra()
    {
        var world = Board();
        var madame = world.CreateCard(
            "01181", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.True(runner.CanTakeDamage(world, madame, source));

        var legions = world.CreateCard("01180", world.AreaOf(DeckType.SideSchemesArea));
        Assert.False(runner.CanTakeDamage(world, madame, source));

        World.MoveToTop(legions, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.True(runner.CanTakeDamage(world, madame, source));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:cannot")]
    [Fact]
    public void UltronIsProtectedExactlyWhileADroneIsInPlay()
    {
        var world = Board();
        var ultron = world.CreateCard("01136", world.AreaOf(DeckType.VillainArea));
        var source = world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.True(runner.CanTakeDamage(world, ultron, source));

        var drone = world.CreateCard(
            "01087", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: 0));
        drone.TurnFaceDown();
        Assert.False(runner.CanTakeDamage(world, ultron, source));

        World.MoveToTop(
            drone, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.True(runner.CanTakeDamage(world, ultron, source));
    }

    private static World Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        return world;
    }
}
