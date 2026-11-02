using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Galactic Artifacts attachments on synthetic boards.
public sealed class GalacticArtifactsTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:attachment.1")]
    public void CloakOfHerculesAttachesToTheEnemyWithTheLowestAttack()
    {
        // 16122: "Attach to the enemy with the lowest ATK." Kree Private (ATK 1) gets +3 ATK.
        var world = Board();
        var private_ = GrootBoards.Engaged(world, KreePrivate);
        var cloak = ShipCommandBoards.Reveal(world, CloakOfHercules);

        Drive(world);

        Assert.Equal(private_.ObjectId, cloak.Area.Host);
        Assert.Equal(4, Modified(world, private_, "attack"));
    }

    [Fact]
    [Rule("rr:attach-to")]
    public void CloakOfHerculesIsDiscardedForThreePhysicalResources()
    {
        // "Hero Action: Spend [physical][physical][physical] resources → discard this card."
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var cloak = ShipCommandBoards.Attach(world, villain, CloakOfHercules);
        var strength = GrootBoards.Hand(world, ShipCommandBoards.Strength);
        var backflip = GrootBoards.Hand(world, "01003");

        Assert.Equal(5, Modified(world, villain, "attack"));
        ShipCommandBoards.Act(world, cloak, 0, strength, backflip);

        Assert.Equal(DeckType.EncounterDiscardPile, cloak.Area.Type);
        Assert.Equal(2, Modified(world, villain, "attack"));
    }

    [Fact]
    [Rule("rr:attachment.1.2")]
    public void ObediencePotionWeakensTheRevealersHero()
    {
        // 16123: "Attach to your identity. Attached character gets -1 THW, -1 ATK, and -1 DEF."
        var world = Board(players: 2);
        var hero = world.Seats[1].IdentityCard;
        var potion = ShipCommandBoards.Reveal(world, ObediencePotion, player: 1);

        Drive(world);

        Assert.Equal(hero.ObjectId, potion.Area.Host);
        Assert.Equal(1, Modified(world, hero, "attack"));
        Assert.Equal(0, Modified(world, hero, "thwart"));
        Assert.Equal(2, Modified(world, hero, "defense"));
    }

    [Fact]
    [Rule("rr:attachment.2.1")]
    public void AnyPlayerMayTakeOneDamageAndSpendTwoMentalToDiscardObediencePotion()
    {
        // "Hero Action: Take 1 damage and spend [mental][mental] resources → discard this card.
        // Any player can do this." (rulings fc47a4c917f820fc, 01adfa3a8a285689). Player 0 helps
        // player 1 and pays with their own identity and hand.
        var world = Board(players: 2);
        var potion = ShipCommandBoards.Attach(world, world.Seats[1].IdentityCard, ObediencePotion);
        var genius = GrootBoards.Hand(world, ShipCommandBoards.Genius);

        ShipCommandBoards.Act(world, potion, 0, genius);

        Assert.Equal(DeckType.EncounterDiscardPile, potion.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:attach-to")]
    public void TheBeyondersBlazerAttachesToTheEnemyWithTheHighestScheme()
    {
        // 16124: "Attach to the enemy with the highest SCH." Kree Lieutenant (SCH 2) outranks Rhino (SCH 1).
        var world = Board();
        var lieutenant = GrootBoards.Engaged(world, KreeLieutenant);
        var blazer = ShipCommandBoards.Reveal(world, BeyondersBlazer);

        Drive(world);

        Assert.Equal(lieutenant.ObjectId, blazer.Area.Host);
    }

    [Fact]
    [Rule("rr:cost-arrow-icon")]
    public void TheBeyondersBlazerCostsTwoThreatOnTheMainSchemeAndTwoResources()
    {
        // "Hero Action: Place 2 threat on the main scheme and spend 2 resources → discard this card."
        var world = Board();
        var scheme = GrootBoards.MainScheme(world);
        var blazer = ShipCommandBoards.Attach(world, GrootBoards.Villain(world), BeyondersBlazer);
        var energy = GrootBoards.Hand(world, ShipCommandBoards.Energy);

        ShipCommandBoards.Act(world, blazer, 0, energy);

        Assert.Equal(DeckType.EncounterDiscardPile, blazer.Area.Type);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void TheBeyondersBlazersActionNeedsAMainSchemeToPlaceItsThreat()
    {
        // "Pay the cost(s). If this step is reached and the cost(s) cannot be paid, abort this
        // process without paying any costs." With no main scheme the threat cannot be placed,
        // so the action is not offered.
        var world = Board();
        var blazer = ShipCommandBoards.Attach(world, GrootBoards.Villain(world), BeyondersBlazer);
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        Assert.Single(ShipCommandBoards.ActionsOn(world, blazer, 0));

        World.MoveToTop(GrootBoards.MainScheme(world), world.AreaOf(DeckType.RemovedArea));

        Assert.Empty(ShipCommandBoards.ActionsOn(world, blazer, 0));
    }

    [Fact]
    [Rule("rr:attach-to")]
    public void VandarianPowerStoneAttachesToTheEnemyWithTheLowestSchemeAndLeavesForThreeEnergy()
    {
        // 16126: "Attach to the enemy with the lowest SCH. Hero Action: Spend
        // [energy][energy][energy] resources → discard this card." Kree Lieutenant has SCH 2.
        var world = Board();
        GrootBoards.Engaged(world, KreeLieutenant);
        var stone = ShipCommandBoards.Reveal(world, VandarianPowerStone);
        Drive(world);
        var energy = GrootBoards.Hand(world, ShipCommandBoards.Energy);
        var aunt = GrootBoards.Hand(world, "01006");

        Assert.Equal(GrootBoards.Villain(world).ObjectId, stone.Area.Host);
        ShipCommandBoards.Act(world, stone, 0, energy, aunt);

        Assert.Equal(DeckType.EncounterDiscardPile, stone.Area.Type);
    }

    [Fact]
    [Rule("rr:hazard-icon")]
    public void VandarianPowerStonesHazardIconDealsOneMoreEncounterCard()
    {
        // "For each hazard icon on cards in play, deal one player one additional card."
        var world = Board();
        ShipCommandBoards.Attach(world, GrootBoards.Villain(world), VandarianPowerStone);

        Assert.Equal(1, Deal.HazardIcons(world, world.Facts));
    }
}
