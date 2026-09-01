using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCardDslTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:ready")]
    [Fact]
    public void ArcReactorExhaustsAndReadiesIronMan()
    {
        // "Exhaust Arc Reactor → ready Iron Man." Readying changes only the
        // exhausted state, and the exhaust before the arrow is paid first.
        var world = Hero("01029a");
        var reactor = world.CreateCard(
            "01035", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.Seats[0].IdentityCard.Exhaust();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == reactor.ObjectId);
        runner.Act(world, action, [], []);

        Assert.False(reactor.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void JessicaJonesCountsTheSideSchemesCurrentlyInPlay()
    {
        // "+1 THW for each side scheme in play" is continuous: adding and
        // removing a scheme changes the modifier without replaying the ally.
        var world = Hero("01029a");
        var jessica = world.CreateCard(
            "01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = AuthoredCards.Runner();

        Assert.Equal(1, Modified(world, jessica, "thwart"));

        var scheme = world.CreateCard("01138", world.AreaOf(DeckType.SideSchemesArea));
        Assert.Equal(2, Modified(world, jessica, "thwart"));

        World.MoveToTop(scheme, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(1, Modified(world, jessica, "thwart"));
    }

    [Rule("rr:traits")]
    [Rule("rr:modifiers")]
    [Fact]
    public void IronManCountsPrintedAndGrantedTechTraitsAndCapsAtSeven()
    {
        // "For each Tech upgrade you control" reads the current trait, not
        // only the ink. The constant hand-size effect therefore settles after
        // a lasting effect grants TECH to another controlled upgrade.
        var world = Hero("01029a");
        world.Abilities = AuthoredCards.Runner();
        var upgrades = world.AreaOf(
            DeckType.UpgradesArea,
            PlayArea.Of(0),
            host: world.Seats[0].IdentityCard.ObjectId,
            cardOwner: 0);
        world.CreateCard("01036", upgrades); // printed TECH
        var granted = world.CreateCard("01093", upgrades); // no printed TECH
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Traits.Granted + "TECH",
            Amount: 1,
            Card: granted.ObjectId,
            Affects: granted.ObjectId));

        Assert.Equal(3, Modified(world, world.Seats[0].IdentityCard, "hand_size"));

        foreach (string face in new[] { "01035", "01037", "01038", "01038", "01039", "01039" })
        {
            world.CreateCard(face, upgrades);
        }

        Assert.Equal(7, Modified(world, world.Seats[0].IdentityCard, "hand_size"));
    }

    [Rule("rr:draw-drawing-cards")]
    [Fact]
    public void AvengersMansionDrawsForTheChosenPlayersIdentity()
    {
        // "Choose a player. That player draws 1 card." The chosen identity
        // carries its owner's seat through the suspended choice.
        var world = Hero("01029a", players: 2);
        var mansion = world.CreateCard(
            "01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var drawn = world.CreateCard("01087", world.Seats[1].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == mansion.ObjectId);

        runner.Act(world, action, [], []);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, mansion, 0, waiting.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), waiting.Tier);

        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
        Assert.Equal(1, drawn.Owner);
        Assert.False(mansion.Ready);
    }

    [Rule("rr:remaining-hit-points")]
    [Rule("rr:remaining-hit-points.2")]
    [Fact]
    public void TitaniasAttackTracksHerRemainingHitPoints()
    {
        // "Start with the character's maximum hit points [...] and subtract
        // their sustained damage." Titania's six minus four is two, and her X
        // follows that remaining-hit-point calculation.
        var world = Hero("01029a");
        var titania = world.CreateCard(
            "01162", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Abilities = AuthoredCards.Runner();

        Assert.Equal(6, Modified(world, titania, "attack"));

        titania.TakeDamage(4);

        Assert.Equal(2, Modified(world, titania, "attack"));
    }

    [Rule("rr:each-player")]
    [Rule("rr:draw-drawing-cards")]
    [Rule("rr:draw-drawing-cards.1")]
    [Fact]
    public void MariaHillHasEachPlayerDraw()
    {
        // "Each player draws 1 card" is one resolution in player order, not
        // another copy of the resolving player's draw. Each drawn card is
        // "added to a player's hand", which both cards visibly are.
        var world = Hero("01029a", players: 2);
        var maria = world.CreateCard(
            "01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("01087", world.Seats[0].Deck);
        var second = world.CreateCard("01087", world.Seats[1].Deck);
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(
            1, [Steps.CardPlayed], Subject: maria.ObjectId, Player: 0);

        runner.Resolve(
            world, occurrence,
            new PendingAbility(maria.ObjectId, AbilityType.Response, 0), [], []);

        Assert.Equal(DeckType.HandsArea, first.Area.Type);
        Assert.Equal(DeckType.HandsArea, second.Area.Type);
    }

    [Rule("rr:target.2.3")]
    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void MariaHillSkipsAPlayerWhoseDeckIsCurrentlyEmpty()
    {
        // The other player's nonempty deck is one valid target, so the group
        // draw resolves. The first player's empty deck is not a valid target
        // at initiation and is skipped rather than replenished from discard.
        var world = Hero("01029a", players: 2);
        var maria = world.CreateCard(
            "01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var firstDiscard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var staysDiscarded = world.CreateCard("01087", firstDiscard);
        var secondDraws = world.CreateCard("01087", world.Seats[1].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = new Occurrence(
            1, [Steps.CardPlayed], Subject: maria.ObjectId, Player: 0);

        runner.Resolve(
            world, occurrence,
            new PendingAbility(maria.ObjectId, AbilityType.Response, 0), [], []);

        Assert.Equal(DeckType.DiscardPile, staysDiscarded.Area.Type);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.HandsArea, secondDraws.Area.Type);
    }

    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void HeartShapedHerbSkipsItsEmptyMinionGroup()
    {
        // The villain is a valid Tough target, so the whole ability resolves.
        // With no engaged minions, its second group contributes no target and
        // no failure after the villain has already changed state.
        var world = Hero("01029a");
        var herb = world.CreateCard(
            "01158", world.AreaOf(DeckType.RevealingArea));
        var villain = world.CreateCard(
            "01094", world.AreaOf(DeckType.VillainArea));

        AuthoredCards.Runner().WhenRevealed(world, herb, 0);

        Assert.True(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Empty(world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:heal")]
    [Fact]
    public void RechannelRequiresAndPaysItsHealingCostBeforeDrawing()
    {
        // The damage removal is left of the arrow. An undamaged Captain Marvel
        // cannot initiate Rechannel; once damaged, both costs precede the draw.
        var world = Hero("01010a");
        var payment = world.CreateCard("01087", world.Seats[0].Hand);
        var drawn = world.CreateCard("01087", world.Seats[0].Deck);
        world.CreateCard("01087", world.Seats[0].Deck);
        world.CreateCard("01087", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.DoesNotContain(runner.Actions(world, 0), action =>
            action.Card == world.Seats[0].IdentityCard.ObjectId);

        world.Seats[0].IdentityCard.TakeDamage(2);
        var action = Assert.Single(runner.Actions(world, 0), action =>
            action.Card == world.Seats[0].IdentityCard.ObjectId);
        runner.Act(world, action, [payment.ObjectId], []);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Rule("rr:prevent")]
    [Rule("rr:prevent.1")]
    [Rule("rr:prevent.1.1")]
    [Fact]
    public void CosmicFlightPreventsExactlyThreeDamage()
    {
        // Preventing damage "reduce[s] the amount of damage the target takes";
        // the amount "dealt" is not reduced. Five is still dealt, while only
        // two is taken after Cosmic Flight prevents three.
        var world = Hero("01010a");
        var flight = world.CreateCard(
            "01017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01087", world.Seats[0].Deck);
        var villain = world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = Occurrence.ForAttack(
            1, [Steps.DamageWouldBeDealt], world, Cards, villain.ObjectId,
            world.Seats[0].IdentityCard.ObjectId, player: 0);
        var interrupt = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == flight.ObjectId);

        runner.Resolve(world, occurrence, interrupt, [], []);

        Assert.Equal(5, runner.WouldBeDealt(
            world, world.Seats[0].IdentityCard, villain, 5, []));
        Assert.Equal(2, runner.WouldTake(
            world, world.Seats[0].IdentityCard, villain, 5, []));
        Assert.Equal(DeckType.DiscardPile, flight.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:damage.step.3")]
    [Fact]
    public void ToughExpiresPreventionCommittedToThatDamageInstance()
    {
        // Cosmic Flight has already committed its one-use prevention when
        // Tough stops the whole instance at step 2. Both defenses expire with
        // that damage; the prevention cannot leak into a later instance.
        var world = Hero("01010a");
        var flight = world.CreateCard(
            "01017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        var hero = world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Statuses.Give(world, hero, Statuses.Tough);
        var occurrence = Occurrence.ForAttack(
            1, [Steps.DamageWouldBeDealt], world, Cards,
            villain.ObjectId, hero.ObjectId, player: 0);
        var interrupt = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == flight.ObjectId);
        runner.Resolve(world, occurrence, interrupt, [], []);

        Damage.Deal(world, Cards, villain, hero, 5, "test", "Damage", []);
        Damage.Deal(world, Cards, villain, hero, 2, "test", "Damage", []);

        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(2, hero.Damage);
        Assert.DoesNotContain(
            world.Effects.Active(), effect => effect.Kind == "preventDamage");
    }

    [Rule("rr:printed")]
    [Fact]
    public void SplitPersonalityDrawsToTheNewFacesPrintedHandSize()
    {
        var world = Hero("01019a,01019b");
        var split = world.CreateCard("01025", world.Seats[0].Hand);
        var payments = Enumerable.Range(0, 3)
            .Select(_ => world.CreateCard("01087", world.Seats[0].Hand)).ToList();
        for (int card = 0; card < 8; card++)
        {
            world.CreateCard("01087", world.Seats[0].Deck);
        }
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "hand_size", Amount: 3,
            Card: world.Seats[0].IdentityCard.ObjectId,
            Affects: world.Seats[0].IdentityCard.ObjectId));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == split.ObjectId);

        runner.Act(world, action, [.. payments.Select(card => card.ObjectId)], []);

        Assert.Equal("01019b", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(6, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, split.Area.Type);
    }

    [Rule("rr:when-defeated-abilities")]
    [Rule("rr:deal-deal-an-encounter-card")]
    [Fact]
    public void HydraSoldierDealsOnlyItsEngagedPlayerAnEncounterCard()
    {
        var world = Hero("01010a", players: 2);
        var soldier = world.CreateCard(
            "01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var encounter = world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(
            1, [Steps.CardDefeated], Subject: soldier.ObjectId, Player: 0);

        runner.Resolve(
            world, occurrence,
            new PendingAbility(soldier.ObjectId, AbilityType.WhenDefeated, 0), [], []);

        Assert.Equal(DeckType.DealtEncounterCardsDeck, encounter.Area.Type);
        Assert.Equal(1, encounter.Area.PlayArea.Player);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    private static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    private static World Hero(string faceId, int players = 1)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            string identity = player == 0 ? faceId : "01001a";
            seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        }

        return world;
    }
}
