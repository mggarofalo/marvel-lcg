using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SpiderManCardsTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:printed")]
    [Fact]
    public void BlackCatRecoversOnlyCardsWithAPrintedMentalResource()
    {
        // "Add each card with a printed mental resource discarded this way."
        var world = Deal();
        var runner = AuthoredCards.Runner();
        var cat = world.CreateCard(
            AuthoredCards.BlackCat,
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var energy = world.CreateCard("01088", world.Seats[0].Deck);
        var genius = world.CreateCard("01089", world.Seats[0].Deck);
        var occurrence = new Occurrence(
            1, [Steps.CardPlayed], Subject: cat.ObjectId, Player: 0);

        runner.Resolve(
            world, occurrence,
            new PendingAbility(cat.ObjectId, AbilityType.ForcedResponse, 0), [], []);

        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.HandsArea, genius.Area.Type);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:play-put-into-play.2")]
    [Fact]
    public void SwingingWebKickPaysChoosesDamagesAndThenDiscards()
    {
        // An Action on an event is played from hand. It remains there while
        // its target question is waiting, then goes to discard after damage.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0));

        runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []);
        Assert.Equal(DeckType.HandsArea, kick.Area.Type);

        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, kick, 0, waiting.Index, Decision.Take(villain.ObjectId), waiting.Tier);

        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
    }

    [Rule("rr:prevent")]
    [Fact]
    public void BackflipPreventsTheImminentAttackDamage()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var backflip = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(
            1,
            [Steps.DamageWouldBeDealt],
            world,
            Cards,
            world.TheCardIn(DeckType.VillainArea)!.ObjectId,
            hero.ObjectId,
            player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == backflip.ObjectId);

        runner.Resolve(world, occurrence, ability, [], []);

        Assert.Equal(0, runner.WouldBeDealt(world, hero, hero, 7, []));
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
    }

    [Rule("rr:cancel.4")]
    [Fact]
    public void EnhancedSpiderSenseCancelsOnlyTheTreacheriesWhenRevealedEffect()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var sense = world.CreateCard(AuthoredCards.EnhancedSpiderSense, world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var treachery = world.CreateCard(
            AuthoredCards.ImTough, world.AreaOf(DeckType.RevealingArea));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var occurrence = new Occurrence(
            1, [Steps.CardRevealed], Subject: treachery.ObjectId, Player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == sense.ObjectId);

        runner.Resolve(world, occurrence, ability, [payment.ObjectId], []);
        runner.WhenRevealed(world, treachery, 0);

        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(DeckType.RevealingArea, treachery.Area.Type);
        Assert.Equal(DeckType.DiscardPile, sense.Area.Type);
    }

    [Rule("rr:uses-x-type")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void WebShooterPaysItsCostsAndLeavesWhenTheLastCounterIsRemoved()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var shooter = world.CreateCard(
            AuthoredCards.WebShooter,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                world.Seats[0].IdentityCard.ObjectId, 0));
        Reveal.EnterPlay(world, Cards, shooter, []);
        shooter.PlaceTokens("c_web", -2);

        string generated = runner.UseResource(world, 0, shooter.ObjectId, []);

        Assert.Equal("W", generated);
        Assert.False(shooter.Ready);
        Assert.Equal(0, shooter.Tokens.GetValueOrDefault("c_web"));
        Assert.Equal(DeckType.DiscardPile, shooter.Area.Type);
    }

    [Rule("rr:resource-ability")]
    [Fact]
    public void WebShooterIsAvailableOnlyInHeroFormAndWhileItsCostsCanBePaid()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var shooter = world.CreateCard(
            AuthoredCards.WebShooter,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                world.Seats[0].IdentityCard.ObjectId, 0));
        Reveal.EnterPlay(world, Cards, shooter, []);

        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);

        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        Assert.Contains(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);

        runner.UseResource(world, 0, shooter.ObjectId, []);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);
    }

    [Rule("rr:stun-stunned.1")]
    [Fact]
    public void WebbedUpDiscardsItselfAndStunsItsEnemy()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var webbed = world.CreateCard(
            AuthoredCards.WebbedUp,
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId, 0));
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            villain.ObjectId, world.Seats[0].IdentityCard.ObjectId, player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == webbed.ObjectId);

        runner.Resolve(world, occurrence, ability, [], []);

        Assert.Equal(DeckType.DiscardPile, webbed.Area.Type);
        Assert.True(Statuses.Has(world, villain, Statuses.Stunned));
    }

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void SpiderTracerLetsItsControllerChooseTheScheme()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard(
            AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard(
            AuthoredCards.SpiderTracer,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId, 0));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        var occurrence = new Occurrence(
            1, [Steps.CardDefeated], Subject: minion.ObjectId, Player: 0);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));

        runner.Resolve(world, occurrence, ability, [], []);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, tracer, 0, waiting.Index, Decision.Take(scheme.ObjectId), waiting.Tier);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:attach-to")]
    [Fact]
    public void TheTwoAttachmentsExposeTheirPrintedLegalHosts()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard(
            AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard(AuthoredCards.SpiderTracer, world.Seats[0].Hand);
        var webbed = world.CreateCard(AuthoredCards.WebbedUp, world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Assert.Equal([minion.ObjectId], runner.AttachmentTargets(world, tracer));
        Assert.Equal(
            [villain.ObjectId, minion.ObjectId],
            runner.AttachmentTargets(world, webbed));
    }

    private static World Deal(bool hero = false)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"], 12345, AuthoredCards.Runner());
        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }
        return world;
    }

    private static void EmptyHand(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[0].Deck);
        }
    }
}
