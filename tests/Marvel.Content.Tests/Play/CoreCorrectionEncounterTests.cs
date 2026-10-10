using Marvel.Content.Tests.Cards;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCorrectionEncounterTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.EncounterDiscardPile)]
    [InlineData(DeckType.SideSchemesArea)]
    [Rule("rr:search")]
    public void KlawTwoRevealsTheImmortalKlawFromEitherSearchArea(DeckType area)
    {
        // A search permits the player "to look at each of the cards in the searched area."
        World world = WorldSetup.DealWithoutCardAbilities(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "klaw", ["spider_man"]), Cards), ["Spider-Man"], Seed);
        Card immortal = Assert.Single(world.Cards, card => card.FaceId == "01127");
        World.MoveToTop(immortal, world.AreaOf(area));
        if (area == DeckType.SideSchemesArea) immortal.PlaceTokens("k_threat", 3);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card klaw = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal("01113", klaw.FaceId);
        klaw.TakeDamage(DamagePlacement.Health(world, Cards, klaw) - 1);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        int[] before = [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];
        BasicPowerInitiation.BasicAttack(world, Cards, 0, klaw, []);
        Agendas.Finish(world, Cards, runner);
        Card next = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal("01114", next.FaceId);
        // "Search" examines only the named areas. A copy already in play stays
        // there without a second reveal or duplicate; the deck is shuffled.
        Assert.Equal(DeckType.SideSchemesArea, immortal.Area.Type);
        Assert.Equal(3, immortal.Tokens["k_threat"]);
        Assert.Equal(Cards.PrintedValue("01114", "HP", 1) + 10, DamagePlacement.Health(world, Cards, next));
        Assert.Single(world.Cards, card => card.FaceId == "01127");
        Assert.NotEqual(before, world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId));
    }

    [Fact]
    [Rule("rr:search")]
    public void LegionsContinuesWhenMadameHydraIsOutsideTheSearchAreas()
    {
        // A search permits the player "to look at each of the cards in the searched area."
        World world = Deal();
        world.CreateCard("01181", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Card legions = world.CreateCard("01180", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.WhenRevealed(world, legions, 0);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        // The specified search areas contain no Madame Hydra; the independent
        // instruction still adds 2 threat for the HYDRA enemy in play.
        Assert.Equal(2, legions.Tokens["k_threat"]);
        Assert.DoesNotContain(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards,
            card => card.FaceId == "01181");
    }

    [Fact]
    [Rule("rr:traits")]
    public void UltronThreeIncludesPrintedFaceupDroneMinions()
    {
        World world = Deal();
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.VillainDeck));
        Card ultron = world.CreateCard("01136", world.AreaOf(DeckType.VillainArea));
        Card drone = world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Abilities = AuthoredCards.Runner();
        // "Some card abilities reference cards that possess or lack specific traits."
        // Ultron III says each DRONE minion, without a facing restriction.
        Assert.Equal(Cards.PrintedValue(drone.FaceId, "ATK", 1) + 1,
            StateFields.Modified(world, drone, "attack", Cards, 1));
        Assert.Equal(Cards.PrintedValue(drone.FaceId, "HP", 1) + 1,
            DamagePlacement.Health(world, Cards, drone));
        DamagePlacement.Deal(world, Cards, world.Seats[0].IdentityCard, ultron, 3, "test", "test", []);
        Assert.Equal(0, ultron.Damage);
        World.MoveToTop(drone, world.AreaOf(DeckType.EncounterDiscardPile));
        DamagePlacement.Deal(world, Cards, world.Seats[0].IdentityCard, ultron, 3, "test", "test", []);
        Assert.Equal(3, ultron.Damage);
    }

    [Fact]
    [Rule("rr:traits")]
    public void DroneFactoryCountsPrintedAndNewFacedownDrones()
    {
        // "Some card abilities reference cards that possess or lack specific traits."
        World world = Deal();
        world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Card factory = world.CreateCard("01148", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.WhenRevealed(world, factory, 0);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Single(AuthoredCards.FacedownDrones(world));
        Assert.Equal(2, factory.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, true)]
    [Rule("rr:you-your.2")]
    [Rule("rr:overkill.2")]
    [Rule("rr:damage.3.2")]
    public void SonicBoomChecksDamageDealtToTheIdentity(bool identityTarget, bool overkill, bool tough, bool exhausted)
    {
        World world = Deal("iron_man");
        Card identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01029a");
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Activation = new EnemyActivation(villain.ObjectId, 0, true, Id: 19);
        Card boost = world.CreateCard("01123", world.AreaOf(DeckType.BoostingArea));
        runner.Boost(world, boost, 0);
        if (overkill) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Keywords.Overkill, Amount: 1, Card: villain.ObjectId, Affects: villain.ObjectId));
        if (tough) Statuses.Give(world, identity, Statuses.Tough);
        world.Attack = new EnemyAttack(villain.ObjectId, 0, identityTarget ? identity.ObjectId : ally.ObjectId,
            Defender: identityTarget ? -1 : ally.ObjectId, CalculatedDamage: overkill ? 5 : 1);
        world.Agenda.Add(new PhaseStep(Steps.DealAttackDamage, 1, 5, Subject: villain.ObjectId));
        world.Agenda.Begin(world, Cards);
        Attack.DealDamage(world, Cards, []);
        EnemyActivation result = world.Activation!;
        runner.ActivationCompleted(world, result);
        // Damage to you "applies it to the hit point dial of their identity."
        // Overkill "is considered damage from an attack" (.2). When damage taken
        // is modified, "the amount of damage dealt is not modified" (damage.3.2).
        Assert.Equal(exhausted, !identity.Ready);
        Assert.Equal(exhausted, result.DamageRecipients.Contains(identity.ObjectId));
        Assert.Equal(tough ? 0 : identityTarget ? 1 : overkill ? 3 : 0, identity.Damage);
    }
}
