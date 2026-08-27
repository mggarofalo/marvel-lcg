using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Step 6 of dealing damage — <c>rr:damage.step.6</c> — through the core-set
/// card Biomechanical Upgrades.
/// </summary>
public sealed class WouldBeDefeatedTests
{
    private const string Mercenary = "01101";
    private const string Sandman = "01102";
    private const string Modok = "01184";

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Fact]
    public void LethalDamageIsHealedAndTheAttachmentIsDiscardedInstead()
    {
        var (world, minion, upgrade) = Board();

        bool defeated = Damage.Deal(world, Cards, minion, 3, "test", "test", []);

        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void NonlethalDamageDoesNotTriggerTheInterrupt()
    {
        var (world, minion, upgrade) = Board();

        bool defeated = Damage.Deal(world, Cards, minion, 2, "test", "test", []);

        Assert.False(defeated);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void ToughPreventsDamageBeforeDefeatBecomesImminent()
    {
        var (world, minion, upgrade) = Board();
        Statuses.Give(world, minion, Statuses.Tough);

        Damage.Deal(world, Cards, minion, 3, "test", "test", []);

        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void TheInterruptOnlyAnswersForItsHost()
    {
        var (world, _, upgrade) = Board();
        var other = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Agendas.Happening(world);

        bool defeated = Damage.Deal(world, Cards, other, 3, "test", "test", []);

        Assert.True(defeated);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:attach-to")]
    [Fact]
    public void ItAttachesToTheHighestPrintedHitPointsWithoutAnotherCopy()
    {
        var world = Empty();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var lower = world.CreateCard(
            Sandman, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var higher = world.CreateCard(
            Modok, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "health",
            Amount: 10,
            Card: lower.ObjectId,
            Affects: lower.ObjectId));
        var first = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));

        Assert.Equal(higher.ObjectId, world.Abilities.AttachesTo(world, first));

        World.MoveToTop(
            first,
            world.AreaOf(DeckType.UpgradesArea, higher.Area.PlayArea, higher.ObjectId));
        var second = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));

        Assert.Equal(lower.ObjectId, world.Abilities.AttachesTo(world, second));
    }

    [Rule("rr:first-player.1")]
    [Fact]
    public void AHighestHitPointTieRefusesToChooseForTheFirstPlayer()
    {
        var world = Empty();
        world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => world.Abilities.AttachesTo(world, upgrade));

        Assert.Contains("rr:first-player.1", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:attach-to.3")]
    [Fact]
    public void WithNoMinionThereIsNothingToAttachTo()
    {
        var world = Empty();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));

        Assert.Null(world.Abilities.AttachesTo(world, upgrade));
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void AnOptionalInterruptIsRefusedRatherThanSilentlyUsed()
    {
        var world = Empty(
            """
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                         "subject":"attachedTo"},
              "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
            }]}]}
            """);
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Damage.Deal(world, Cards, minion, 3, "test", "test", []));

        Assert.Contains("optional interrupt", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("rr:damage.step.6", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:forced.4")]
    [Rule("rr:would.1")]
    [Fact]
    public void AForcedReplacementInvalidatesALaterOptionalInterrupt()
    {
        var world = Empty(
            """
            {"cards":[
              {"card":"01185","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"attachedTo"},
                "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                           "subject":"this"},
                "effect":{"discard":"this"}
              }]}
            ]}
            """);
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));

        bool defeated = Damage.Deal(world, Cards, minion, 3, "test", "test", []);

        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void AForcedInterruptThatDoesNotPreventDefeatLeavesItImminent()
    {
        var world = Empty(
            """
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                         "subject":"attachedTo"},
              "effect":{"discard":"this"}
            }]}]}
            """);
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        Agendas.Happening(world);

        bool defeated = Damage.Deal(world, Cards, minion, 3, "test", "test", []);

        Assert.True(defeated);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, upgrade.Area.Type);
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TwoForcedInterruptsRefuseToChooseTheFirstPlayersOrder()
    {
        var world = Empty(
            """
            {"cards":[
              {"card":"01185","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"attachedTo"},
                "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"this"},
                "effect":{"discard":"this"}
              }]}
            ]}
            """);
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Damage.Deal(world, Cards, minion, 3, "test", "test", []));

        Assert.Contains("rr:forced.5", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, minion.Damage);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    private static (World World, Card Minion, Card Upgrade) Board()
    {
        var world = Empty();
        var minion = world.CreateCard(
            Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(
            AuthoredCards.BiomechanicalUpgrades,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        return (world, minion, upgrade);
    }

    private static World Empty(string? abilities = null)
    {
        var world = new World(Cards, players: 1);
        world.CreateSeat("p0");
        world.Abilities = abilities is null
            ? AuthoredCards.Runner()
            : new AbilityRunner(AbilityCatalog.Parse(abilities));
        return world;
    }
}
