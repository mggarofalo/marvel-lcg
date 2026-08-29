using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreActivationAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:play-put-into-play")]
    [Fact]
    public void LegionsOfHydraPutsMadameHydraIntoPlayEngagedWithTheRevealingPlayer()
    {
        // The printed side scheme says to put Madame Hydra into play engaged
        // with the player who revealed it. The authored effect uses the DSL's
        // reusable `where: engagedWithYou` destination.
        var world = Board("01001a", "01094");
        var legions = world.CreateCard("01180", world.AreaOf(DeckType.SideSchemesArea));
        var madame = world.CreateCard("01181", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.WhenRevealed(world, legions, 0);

        Assert.Equal(DeckType.EngagedEnemiesArea, madame.Area.Type);
        Assert.Equal(PlayArea.Of(0), madame.Area.PlayArea);
    }

    [Rule("rr:forced.6")]
    [Fact]
    public void MadameHydrasCompletedActivationDoesNothingAfterLegionsLeavesPlay()
    {
        // A forced ability resolves as completely as possible. If Legions of
        // Hydra has already left play, "place 2 threat on Legions of Hydra"
        // has no scheme to change and does not invent one or fail the game.
        var world = Board("01001a", "01094");
        var madame = world.CreateCard(
            "01181", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();

        var events = runner.ActivationCompleted(
            world,
            new EnemyActivation(madame.ObjectId, 0, Attacking: true, Made: true));

        Assert.Empty(events);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:boost-boost-icon.4")]
    [Rule("rr:boost-boost-icon.6")]
    [Rule("rr:star-icon.1")]
    [Fact]
    public void KlawGetsAnAdditionalFacedownBoostCardWhenHeAttacks()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var klaw = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var boost = world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            klaw.ObjectId, seat.IdentityCard.ObjectId, 0);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == klaw.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        var waiting = world.AreaOf(
            DeckType.BoostCardsDeck, klaw.Area.PlayArea, host: klaw.ObjectId);
        Assert.Equal(boost.ObjectId, Assert.Single(waiting.Cards).ObjectId);
        Assert.False(boost.FaceUp);
    }

    [Rule("rr:activation.7")]
    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void KlawsVengeanceWaitsToSeeWhetherTheAttackDealtDamage()
    {
        var world = Board("01001a", "01113");
        var scheme = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        var card = world.CreateCard("01122", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;

        runner.ActivationCompleted(
            world, new EnemyActivation(world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                0, Attacking: true, activation, Made: true, DamageDealt: 2));

        var placement = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.PlaceThreatEffect);
        Assert.Equal(scheme.ObjectId, placement.Placement!.Scheme);
        Assert.Equal(1, placement.Placement.Assigned);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void RageOfUltronUsesOnlyTheCompletedSchemesThreat()
    {
        var world = Board("01001b", "01134");
        var first = world.CreateCard("01002", world.Seats[0].Deck);
        var second = world.CreateCard("01003", world.Seats[0].Deck);
        var third = world.CreateCard("01004", world.Seats[0].Deck);
        var card = world.CreateCard("01145", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Scheme).ActivationId;

        runner.ActivationCompleted(
            world, new EnemyActivation(world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                0, Attacking: false, activation, Made: true, ThreatPlaced: 2));

        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        Assert.Single(world.Seats[0].Deck.Cards);
        Assert.Equal(2, discard.Cards.Count);
        Assert.Contains(first, world.Seats[0].Deck.Cards.Concat(discard.Cards));
        Assert.Contains(second, world.Seats[0].Deck.Cards.Concat(discard.Cards));
        Assert.Contains(third, world.Seats[0].Deck.Cards.Concat(discard.Cards));
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SwarmAttackCreatesADroneWhenEveryAttemptedAttackIsCanceled()
    {
        var world = Board("01001a", "01134");
        var original = world.CreateCard(
            "01087",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: 0));
        original.TurnFaceDown();
        world.CreateCard("01002", world.Seats[0].Deck);
        var card = world.CreateCard("01147", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;
        runner.ActivationCompleted(
            world, new EnemyActivation(original.ObjectId, 0, Attacking: true,
                activation, Made: false));

        Assert.Equal(
            2,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:activation.7")]
    [Rule("rr:surge.1")]
    [Fact]
    public void TitaniasFuryHealsAndSurgesWhenStunCancelsHerAttack()
    {
        var world = Board("01001a", "01113");
        var titania = world.CreateCard(
            "01162", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        titania.TakeDamage(3);
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("01164", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;
        runner.ActivationCompleted(
            world, new EnemyActivation(titania.ObjectId, 0, Attacking: true,
                activation, Made: false));

        Assert.Equal(0, titania.Damage);
        Assert.Single(world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0), cardOwner: 0).Cards);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void MadameHydraPlacesThreatOnceAfterEitherKindOfActivation()
    {
        var world = Board("01001b", "01113");
        var legions = world.CreateCard("01180", world.AreaOf(DeckType.SideSchemesArea));
        var madame = world.CreateCard(
            "01181", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Agenda.Add(new PhaseStep(
            Steps.CompleteSchemeActivation, 1, 1,
            Subject: madame.ObjectId, Seat: 0, ActivationId: 7));

        runner.ActivationCompleted(
            world, new EnemyActivation(
                madame.ObjectId, 0, Attacking: false, Id: 7, Made: true, ThreatPlaced: 1));

        var placement = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.PlaceThreatEffect);
        Assert.Equal(legions.ObjectId, placement.Placement!.Scheme);
        Assert.Equal(2, placement.Placement.Assigned);
    }

    [Rule("rr:attack-enemy-activation.1.1")]
    [Fact]
    public void MastersOfMayhemAttacksOnlyEachMinionsOwnEngagedHero()
    {
        var world = new World(Cards, players: 2);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard("01001a", first.Hero);
        var second = world.CreateSeat("p1");
        second.IdentityCard = world.CreateCard("01029b", second.Hero);
        var attacks = world.CreateCard(
            "01129", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            "01131", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var card = world.CreateCard("01133", world.AreaOf(DeckType.RevealingArea));

        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        var activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(attacks.ObjectId, activation.Subject);
        Assert.Equal(0, activation.Seat);
    }

    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    [Fact]
    public void MastersOfMayhemSearchesWhenNoAttackCanBeMade()
    {
        var world = Board("01001b", "01113");
        var found = world.CreateCard("01129", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("01133", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(
            world, card, 0, choice.Index, Decision.Take(found.ObjectId), choice.Tier);

        Assert.Contains(
            found,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:exhausted")]
    [Fact]
    public void SonicBoomOffersOnlyExhaustWhenTheResourceOptionCannotBePaid()
    {
        var world = Board("01001a", "01113");
        var ally = world.CreateCard(
            "01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("01123", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, card, 0, choice.Index, choice.Tier)!;
        var exhaust = Assert.Single(prompt.Affordances);
        Assert.Equal(1, exhaust.Id);
        runner.Chose(world, card, 0, choice.Index, Decision.Take(exhaust.Id), choice.Tier);

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(ally.Ready);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SonicBoomBoostWaitsForThatActivationsDamageResult()
    {
        var world = Board("01001a", "01113");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var card = world.CreateCard("01123", world.AreaOf(DeckType.BoostingArea));
        var runner = AuthoredCards.Runner();
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 12);

        runner.Boost(world, card, 0);
        Assert.True(world.Seats[0].IdentityCard.Ready);

        runner.ActivationCompleted(
            world, new EnemyActivation(
                villain.ObjectId, 0, Attacking: true, Id: 12,
                Made: true, DamageDealt: 1));

        Assert.False(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:attack-enemy-activation")]
    [Fact]
    public void WhirlwindAddsBothOtherHeroesToOneThreePlayerAttack()
    {
        var world = new World(Cards, players: 3);
        for (int player = 0; player < 3; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var whirlwind = world.CreateCard(
            "01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: whirlwind.ObjectId, Seat: 0));
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            whirlwind.ObjectId, world.Seats[0].IdentityCard.ObjectId, 0);

        var response = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.Resolve(world, occurrence, response, [], []);

        Assert.Equal([1, 2], world.PendingAdditionalAttackPlayers);
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void WhirlwindBoostDamagesBothHeroes()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var card = world.CreateCard("01130", world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);

        Assert.Equal([1L, 1L], world.Seats.Select(seat => seat.IdentityCard.Damage));
    }

    private static World Board(string identity, string villain)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard(villain, world.AreaOf(DeckType.VillainArea));
        return world;
    }
}
