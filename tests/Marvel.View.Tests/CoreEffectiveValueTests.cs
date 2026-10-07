using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class CoreEffectiveValueTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly AbilityBook Abilities = AbilityCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));

    [Rule("rr:modifiers")]
    [Fact]
    public void CombatTrainingChangesTheMainAttackAndNamesTheActualSource()
    {
        // Combat Training: "Your hero gets +1 ATK." The live engine value is
        // the displayed quantity; the printed value remains inspectable.
        var world = Board("01040a");
        Card hero = world.Seats[0].IdentityCard;
        Card training = world.CreateCard("01057", Upgrades(world));

        CardFaceDescriptor face = Face(world, hero);
        CardEffectiveValue value = face.EffectiveValues["ATK"];
        Assert.Equal(2, value.BaseValue);
        Assert.Equal(3, value.CurrentValue);
        Assert.Equal(StateFields.Modified(world, hero, "attack", Cards, 1), value.CurrentValue);
        Assert.True(value.IsModified);
        CardValueCalculation step = Assert.Single(value.Calculation);
        Assert.Equal(1, step.Amount);
        Assert.Equal("Combat Training", step.Source!.Title);
        Assert.Equal(training.ObjectId, step.Source.CardId);
        Assert.False(step.Source.Historical);

        World.MoveToTop(training, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        value = Face(world, hero).EffectiveValues["ATK"];
        Assert.Equal(2, value.CurrentValue);
        Assert.False(value.IsModified);
        Assert.Empty(value.Calculation);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void IronManReportsTheResolvedCappedContributionFromHisActualAbility()
    {
        // Iron Man: "+1 hand size for each Tech upgrade you control, to a
        // maximum hand size of 7." The DSL evaluates both count and cap.
        var world = Board("01029a");
        Card hero = world.Seats[0].IdentityCard;
        Assert.Equal(1, Face(world, hero).EffectiveValues["HS"].CurrentValue);
        Card reactor = world.CreateCard("01035", Upgrades(world));
        Assert.Equal(2, Face(world, hero).EffectiveValues["HS"].CurrentValue);
        foreach (string id in new[] { "01036", "01037", "01038", "01038", "01039", "01039" })
            world.CreateCard(id, Upgrades(world));

        CardEffectiveValue value = Face(world, hero).EffectiveValues["HS"];
        Assert.Equal(1, value.BaseValue);
        Assert.Equal(7, value.CurrentValue);
        Assert.Equal(PhaseEnd.HandSize(world, world.Seats[0], Cards), value.CurrentValue);
        CardValueCalculation step = Assert.Single(value.Calculation);
        Assert.Equal(6, step.Amount);
        Assert.Equal("Iron Man", step.Source!.Title);
        Assert.Contains("maximum", step.Source.RulesText, StringComparison.OrdinalIgnoreCase);
        World.MoveToTop(reactor, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Equal(7, Face(world, hero).EffectiveValues["HS"].CurrentValue);
    }

    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void MarkVArmorShowsMaximumAndRemainingHpWithoutHealingDamage()
    {
        // "You get +6 hit points." HP gain raises maximum/remaining equally.
        var world = Board("01029a");
        Card hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        Card armor = world.CreateCard("01036", Upgrades(world));
        CardFaceDescriptor face = Face(world, hero);

        Assert.Equal(15, face.EffectiveValues["HP"].CurrentValue);
        Assert.Equal(9, face.EffectiveValues["HP"].BaseValue);
        Assert.Equal(11, face.Fields["health"]);
        Assert.Equal(4, face.Damage);
        Assert.Equal("Mark V Armor", Assert.Single(face.EffectiveValues["HP"].Calculation).Source!.Title);
        World.MoveToTop(armor, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        face = Face(world, hero);
        Assert.Equal(9, face.EffectiveValues["HP"].CurrentValue);
        Assert.Equal(5, face.Fields["health"]);
        Assert.Equal(4, face.Damage);
    }

    [Rule("rr:per-player-icon")]
    [Fact]
    public void PerPlayerHpIsResolvedBeforeItBecomesTheBaseAndCurrentMaximum()
    {
        // The per-player icon multiplies its value by the number of players.
        var world = new World(Cards, 2, seed: 7);
        world.CreateSeat("P0");
        world.CreateSeat("P1");
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        CardEffectiveValue value = Face(world, rhino).EffectiveValues["HP"];
        Assert.Equal(28, value.BaseValue);
        Assert.Equal(28, value.CurrentValue);
        Assert.False(value.IsModified);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void UpgradedDronesNamesItsAuraAndNeverRevealsThePhysicalDroneCard()
    {
        // Upgraded Drones affects "each facedown Drone minion", not the
        // faceup Advanced Ultron Drone. The public replacement is the base.
        var world = Board("01040a");
        Card environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        Card upgrade = world.CreateCard("01142", world.AreaOf(DeckType.UpgradesArea, host: environment.ObjectId));
        Area engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card drone = world.CreateCard("01091", engaged);
        drone.AssignProfile(AbilityLowering.Book(Abilities).Profiles["effective-drone"]);
        drone.TurnFaceDown();
        Card advanced = world.CreateCard("01143", engaged);

        CardFaceDescriptor face = Face(world, drone);
        Assert.Equal("effective-drone", face.Id);
        Assert.Null(face.ArtFaceId);
        Assert.Equal("Replacement", face.EffectiveValues["ATK"].BaseKind);
        Assert.Equal(2, face.EffectiveValues["ATK"].CurrentValue);
        Assert.Equal(2, face.EffectiveValues["HP"].CurrentValue);
        Assert.Equal(upgrade.ObjectId, Assert.Single(face.EffectiveValues["HP"].Calculation).Source!.CardId);
        Assert.Empty(Face(world, advanced).EffectiveValues["ATK"].Calculation);
        World.MoveToTop(upgrade, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(1, Face(world, drone).EffectiveValues["ATK"].CurrentValue);
    }

    private static World Board(string identity)
    {
        var world = new World(Cards, 1, seed: 7) { Abilities = new AbilityRunner(Abilities) };
        Seat seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        return world;
    }

    private static Area Upgrades(World world) => world.AreaOf(
        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0, host: world.Seats[0].IdentityCard.ObjectId);

    private static CardFaceDescriptor Face(World world, Card card) =>
        VisibilityFixture.Card(WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World, card.ObjectId).Face!;
}
