using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Sinister Syndicate, a modular set that punishes what you have built.
/// </summary>
/// <remarks>
/// <para>
/// Every card in the set is aimed at the player's own board rather than at
/// their hit points — the allies they have played, the upgrades they have
/// attached, the support they are leaning on. That makes it the first set the
/// engine has met whose abilities need to <i>read</i> a player's play area, and
/// the queries here exist for that.
/// </para>
/// <para>
/// It also completes a scenario: <c>2410_need_for_speed</c> is the Rhino board
/// with this set instead of Bomb Scare, and these seven cards were the last of
/// its thirty that nobody had read.
/// </para>
/// </remarks>
public sealed class SinisterSyndicateTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    /// <summary>Black Cat, a two-cost Spider-Man ally.</summary>
    private const string BlackCat = "01002";

    /// <summary>Spider-Woman, a second ally, so that "each" can be wrong.</summary>
    private const string SpiderWoman = "01011";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:attack-enemy-activation.1.4")]
    [Fact]
    public void BoomerangDamagesEveryAllyOfThePlayerItAttacked()
    {
        // "**Forced Response:** After Boomerang attacks you, deal 1 damage to
        // each ally you control." Two allies, one damage each -- and the
        // response is on the attack ending rather than on the damage step,
        // which is where `rr:attack-enemy-activation.step.6.a` puts "after
        // [character] attacks ... you".
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var cat = Ally(world, BlackCat, 0);
        var woman = Ally(world, SpiderWoman, 0);
        var boomerang = world.CreateCard(
            AuthoredCards.Boomerang,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: boomerang.ObjectId, Seat: 0));
        Run(world);

        Assert.Equal(1, cat.Damage);
        Assert.Equal(1, woman.Damage);
    }

    [Rule("rr:you-your.7")]
    [Rule("rr:ability.8")]
    [Fact]
    public void TheYouBoomerangMeansIsTheAttackedPlayerAndNotItsOwner()
    {
        // The rule this needed, spelled out: "for abilities that trigger 'after
        // [enemy] attacks you,' **'you' refers to the attacked player**, even
        // if that player defended with an ally."
        //
        // An encounter card is owned by the scenario, and control is what
        // `PendingAbility` carries -- `rr:ability.8` says any player may use an
        // optional ability on one, so the scenario is the right answer to
        // *whose opportunity is this*. It is the wrong answer to *who does the
        // card mean by you*, and before this the two were the same field. Two
        // seats here, allies on both, and only the attacked one may be hit.
        var world = Deal("spider_man", "spider_man");
        world.Seats[1].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var mine = Ally(world, BlackCat, 0);
        var theirs = Ally(world, SpiderWoman, 1);
        var boomerang = world.CreateCard(
            AuthoredCards.Boomerang,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: boomerang.ObjectId, Seat: 1));
        Run(world);

        Assert.Equal(1, theirs.Damage);
        Assert.Equal(0, mine.Damage);
    }

    [Rule("rr:choose-game-element")]
    [Fact]
    public void BoomerangWithNoAlliesToHitDoesNothingRatherThanStop()
    {
        // The same response on a board with no allies. `dealDamage` over an
        // empty query is nothing happening, which is the right answer -- the
        // card's sentence is about each ally you control and you control none.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var identity = world.Seats[0].IdentityCard;
        var boomerang = world.CreateCard(
            AuthoredCards.Boomerang,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: boomerang.ObjectId, Seat: 0));
        Run(world);

        Assert.True(identity.Damage > 0, "the attack itself still landed");
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void AsABoostCardBoomerangDealsTwoToAnAllyThePlayerPicks()
    {
        // "[star] **Boost:** Deal 2 damage to an ally you control." One ally on
        // the board, so the choice has one answer -- but it is still a choice,
        // and `rr:choose-game-element.1` puts it to the player resolving.
        var world = Deal();
        var cat = Ally(world, BlackCat, 0);
        var card = world.CreateCard(
            AuthoredCards.Boomerang, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);
        Run(world);

        Assert.Equal(2, cat.Damage);
    }

    [Rule("rr:choose-game-element")]
    [Fact]
    public void AsABoostCardWithNoAllyItAsksNothing()
    {
        // `rr:choose-game-element` chooses "a game element that meets the
        // specific requirements of an ability", and a player with no ally has
        // none to offer. The guard is on the card rather than in the
        // interpreter: an unguarded `chooseCard` over an empty board is an
        // authoring error and says so.
        var world = Deal();
        var card = world.CreateCard(
            AuthoredCards.Boomerang, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);

        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Card Ally(World world, string faceId, int seat) =>
        world.CreateCard(faceId, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(seat)));

    /// <summary>
    /// Runs the agenda out. Declines what can be declined and takes the first
    /// answer to what cannot -- a card's own choice is not cancellable, because
    /// the ability is resolving and one of the things it offers will happen.
    /// </summary>
    private static void Run(World world)
    {
        var abilities = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 12, $"'{asked.Label}' is still being asked");
            Sequence.Answer(world, Cards, abilities, asked, Decline(asked), events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
    }

    private static Decision Decline(Prompt asked) =>
        asked.Cancellable ? Decision.Decline : Decision.Take(asked.Affordances[0].Id);

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
