using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Standard set's three treacheries that make an enemy act.
/// </summary>
/// <remarks>
/// <para>
/// <b>"The villain attacks you" is not a call.</b> An attack is the six steps
/// of <c>rr:attack-enemy-activation</c> and one of them asks a player who is
/// defending, so a card that causes one cannot resolve it and return. These
/// cards put the activation on the agenda, which is also what
/// <c>rr:surge.2</c> asks for: the card that caused it finishes resolving
/// first.
/// </para>
/// <para>
/// So what every test below asserts is <i>what is now scheduled</i>, and the
/// last one lets the schedule actually run. The distinction is the bug this
/// would otherwise have: an attack resolved inline would deal its damage before
/// the treachery had finished being revealed, and would have nowhere to stop
/// and ask.
/// </para>
/// </remarks>
public sealed class ActivationCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    /// <summary>Hydra Mercenary — a minion in the Rhino set.</summary>
    private const string HydraMercenary = "01101";

    /// <summary>
    /// She-Hulk's hero face. Not in <c>AuthoredCards</c>: that file names cards
    /// with ability data, and this one is here only to be somebody else's
    /// identity in hero form.
    /// </summary>
    private const string SheHulk = "01019a";

    /// <summary>Sandman — a second minion, so that an order can be wrong.</summary>
    private const string Sandman = "01102";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:scheme-enemy-activation")]
    [Rule("rr:activation.1")]
    [Fact]
    public void AdvanceMakesTheVillainScheme()
    {
        // "When Revealed: The villain schemes." -- and it schemes whatever form
        // the revealing player is in. `rr:activation.1` reads the form to
        // choose between attacking and scheming, but that is the activation the
        // villain phase schedules; this card has already chosen. The board here
        // is in hero form, where an activation would have been an attack.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Reveal(world, AuthoredCards.Advance);

        var step = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.Scheme, step.What);
        Assert.Equal(villain.ObjectId, step.Subject);
    }

    [Rule("rr:form-change-form")]
    [Fact]
    public void AssaultAttacksAHeroAndSurgesAnAlterEgo()
    {
        // Two printed abilities, "(Hero)" and "(Alter-Ego)", and the form
        // decides which. They are written as one `if` because the forms are
        // exclusive -- the parenthesis is a condition on the ability, not two
        // abilities that might both apply.
        var hero = Deal();
        hero.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var villain = hero.TheCardIn(DeckType.VillainArea)!;

        Reveal(hero, AuthoredCards.Assault);

        var step = Assert.Single(hero.Agenda.Outstanding);
        Assert.Equal(Steps.Attack, step.What);
        Assert.Equal(villain.ObjectId, step.Subject);
        Assert.Equal(0, step.Seat);

        // The alter-ego half is a surge, which `rr:surge.1` makes "deal
        // yourself 1 facedown encounter card" -- so it lands in the queue and
        // nothing is scheduled to activate.
        var alterEgo = Deal();
        int queued = Queue(alterEgo).Cards.Count;

        Reveal(alterEgo, AuthoredCards.Assault);

        Assert.Empty(alterEgo.Agenda.Outstanding);
        Assert.Equal(queued + 1, Queue(alterEgo).Cards.Count);
    }

    [Rule("rr:minion.3")]
    [Fact]
    public void GangUpBringsEveryMinionEngagedWithYou()
    {
        // "The villain **and each minion engaged with you** attacks you." Two
        // activations rather than one attack by several enemies: an activation
        // is one enemy's, and a minion engaged with you attacking is what
        // villain phase step 2.b would have done anyway.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var first = world.CreateCard(HydraMercenary, engaged);
        var second = world.CreateCard(Sandman, engaged);

        Reveal(world, AuthoredCards.GangUp);

        // The villain first and the minions after it, in the order the play
        // area holds them. `rr:minion.3` makes that order the player's choice
        // and asking is not implemented, so it is taken deterministically and
        // stated here -- which is exactly how villain phase step 2.b takes it.
        // Two minions rather than one: with one, any order is the right one.
        Assert.Equal(
            [villain.ObjectId, first.ObjectId, second.ObjectId],
            world.Agenda.Outstanding.Select(step => step.Subject));
        Assert.All(world.Agenda.Outstanding, step => Assert.Equal(Steps.Attack, step.What));
    }

    [Rule("rr:engage.1")]
    [Rule("rr:reveal.2")]
    [Fact]
    public void AMinionEngagedWithSomebodyElseIsNotYourGangUp()
    {
        // "engaged with **you**" -- `rr:engage.1` makes engagement which play
        // area the minion sits in, and `rr:reveal.2` makes "you" the player
        // revealing the card. At one player both are seat zero and neither
        // clause can be wrong, which is why this board has two and why the
        // card is revealed by the *second* player: a reading that took the
        // first player, or the play area the ability happens to be looking at,
        // would pass at one player and fail on a table.
        var world = Deal("spider_man", "she_hulk");
        world.Seats[1].IdentityCard.TurnTo(SheHulk);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var mine = world.CreateCard(
            HydraMercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard(
            HydraMercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal(world, AuthoredCards.GangUp, player: 1);

        // The minion in seat one's area and not the one in seat zero's, and
        // both activations aimed at seat one.
        Assert.Equal(
            [villain.ObjectId, mine.ObjectId],
            world.Agenda.Outstanding.Select(step => step.Subject));
        Assert.All(world.Agenda.Outstanding, step => Assert.Equal(1, step.Seat));
    }

    [Rule("rr:surge.2")]
    [Fact]
    public void TheAttackHappensAfterTheRevealRatherThanInsideIt()
    {
        // The property the scheduling exists for. Revealing Assault through the
        // villain phase must leave the villain's attack still to come -- if it
        // resolved inside the reveal, the game would have had to ask who
        // defends from inside an ability that has to return a list of events.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var identity = world.Seats[0].IdentityCard;
        long before = identity.Tokens.GetValueOrDefault("k_damage");

        var events = new List<Marvel.Rules.Events.GameEvent>();
        var card = world.CreateCard(
            AuthoredCards.Assault, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        // Not a scratch on the identity yet, and an attack waiting behind the
        // reveal that caused it.
        Assert.Equal(before, identity.Tokens.GetValueOrDefault("k_damage"));
        Assert.Empty(events);

        // And it belongs to the round the card was revealed in, not to round
        // zero. `Attack.Initiate` hands this round on to all six steps of the
        // activation, and `Moment.Id` builds an occurrence's identity out of
        // it -- so an attack stamped with the wrong round is an attack whose
        // interrupt window claims to be a different moment than it is.
        var attack = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(1, attack.Round);
    }

    private static Area Queue(World world) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));

    /// <summary>Reveals one printed card, as the seat that was dealt it.</summary>
    private static void Reveal(World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, player);
    }

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
