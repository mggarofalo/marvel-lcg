using Marvel.Content.Setup;
using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
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

    /// <summary>Spider-Tracer, a one-cost upgrade.</summary>
    private const string SpiderTracer = "01007";

    /// <summary>Webbed Up, a four-cost upgrade — dearer, so "lowest" can be wrong.</summary>
    private const string WebbedUp = "01009";

    /// <summary>Hulk, an Aggression ally — a card that is not identity-specific.</summary>
    private const string Hulk = "01050";

    /// <summary>Sandman, a Criminal minion of the Rhino set.</summary>
    private const string Sandman = "01102";

    /// <summary>Hydra Mercenary, a minion that is Hydra and not Criminal.</summary>
    private const string HydraMercenary = "01101";

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

    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:tough.3")]
    [Fact]
    public void BeetleTakesTheCheapestUpgradeWhenTheAttackLands()
    {
        // "**Forced Response:** After Beetle attacks and damages you, discard
        // the lowest-cost upgrade you control." One at cost 1 and one at cost
        // 4, and the cheap one goes.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var cheap = Upgrade(world, SpiderTracer, 0);
        var dear = Upgrade(world, WebbedUp, 0);
        var beetle = world.CreateCard(
            AuthoredCards.Beetle, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: beetle.ObjectId, Seat: 0));
        Run(world);

        Assert.Equal(DeckType.DiscardPile, cheap.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, dear.Area.Type);
    }

    [Rule("rr:tough.3")]
    [Fact]
    public void BeetleTakesNothingWhenAToughCardAteTheAttack()
    {
        // "Attacks **and damages** you" is two facts, and `rr:tough.3` is what
        // pulls them apart at a real table: a character whose tough status card
        // absorbed the attack "is not considered to have taken damage". Beetle
        // attacked; Beetle did not damage; the upgrade stays.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        Statuses.Give(world, identity, Statuses.Tough);
        var cheap = Upgrade(world, SpiderTracer, 0);
        var beetle = world.CreateCard(
            AuthoredCards.Beetle, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: beetle.ObjectId, Seat: 0));
        Run(world);

        Assert.Equal(0, identity.Damage);
        Assert.Equal(DeckType.UpgradesArea, cheap.Area.Type);
    }

    [Rule("rr:permanent.4.1")]
    [Fact]
    public void APermanentIsNotTheLowestCostUpgradeHoweverCheapItIs()
    {
        // "If a permanent card would be targeted by such an effect *(for
        // example, 'discard the lowest-cost support you control')*, that effect
        // instead targets the **non-permanent** card that fits its criteria."
        //
        // So a permanent is dropped before the comparison rather than after it.
        // Dropped after, a cheap permanent would be picked, found untouchable,
        // and shield the dearer card the effect should have taken.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var permanent = Upgrade(world, SpiderTracer, 0);
        var dear = Upgrade(world, WebbedUp, 0);
        var beetle = world.CreateCard(
            AuthoredCards.Beetle, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        // Granted on the board rather than printed: Spider-Tracer is not a
        // permanent card, and what is under test is the interpreter's reading
        // of the keyword rather than a card. `rr:permanent.1` makes it "a
        // constant ability", which is the kind of thing a card can grant, so
        // this is also the shape the rule allows.
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "permanent", Amount: 1,
            Card: permanent.ObjectId, Affects: permanent.ObjectId));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: beetle.ObjectId, Seat: 0));
        Run(world);

        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
        Assert.Equal(DeckType.DiscardPile, dear.Area.Type);
    }

    /// <summary>
    /// An upgrade in a player's play area, owned by them.
    /// </summary>
    /// <remarks>
    /// <c>cardOwner</c> and not only <c>PlayArea.Of</c>: they are two questions
    /// — where the area sits, and who a card made there belongs to — and
    /// <c>Discard.Card</c> reads the second to pick which discard pile. An
    /// upgrade made without an owner is discarded to the encounter pile.
    /// </remarks>
    [Rule("rr:attack-enemy-activation.5")]
    [Rule("rr:you-your.7")]
    [Fact]
    public void WhiteRabbitTakesACardFromTheAttackedPlayersHandBeforeTheBoost()
    {
        // "★ **Forced Interrupt:** When White Rabbit attacks you, discard 1
        // card at random from your hand."
        //
        // Two seats, and the hand that shrinks is the attacked player's.
        // `rr:you-your.7` is why: "you" is the attacked player for a trigger of
        // this shape, and the interpreter reads it off the occurrence rather
        // than off who owns the card -- nobody owns White Rabbit.
        var world = Deal("spider_man", "spider_man");
        world.Seats[1].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        int mine = world.Seats[0].Hand.Cards.Count;
        int theirs = world.Seats[1].Hand.Cards.Count;
        var rabbit = world.CreateCard(
            AuthoredCards.WhiteRabbit,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: rabbit.ObjectId, Seat: 1));
        Run(world);

        Assert.Equal(theirs - 1, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(mine, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:identity-specific-card")]
    [Rule("rr:identity-specific-card.3")]
    [Fact]
    public void AsABoostCardWhiteRabbitTakesAnIdentitySpecificCardAndNoOther()
    {
        // "★ **Boost:** Choose and discard 1 identity-specific card from your
        // hand." `rr:identity-specific-card` is a classification -- "cards that
        // belong to an identity's set of accompanying cards", designated by the
        // identity icon in the bottom-right corner -- so an aspect card in the
        // same hand is not a candidate however much the player would rather
        // lose it.
        // Two seats, and the boost resolves for the second. "Your hand" is the
        // hand of the player the activation is against, and the first player's
        // identity-specific card must be left alone -- which is a thing to get
        // wrong, because an encounter card has no owner to read it off.
        var world = Deal("spider_man", "spider_man");
        var hand = Emptied(world, 1);
        var untouched = world.CreateCard(SpiderTracer, Emptied(world, 0));

        var aspect = world.CreateCard(Hulk, hand);
        var specific = world.CreateCard(SpiderTracer, hand);
        var card = world.CreateCard(
            AuthoredCards.WhiteRabbit, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 1);
        Run(world);

        Assert.Equal(DeckType.DiscardPile, specific.Area.Type);
        Assert.Contains(aspect, hand.Cards);
        Assert.Contains(untouched, world.Seats[0].Hand.Cards);
    }

    [Rule("rr:choose-game-element")]
    [Fact]
    public void AHandWithNothingIdentitySpecificIsAskedNothing()
    {
        // An ordinary hand rather than an edge case: a deck is mostly aspect
        // and basic cards, and `rr:target.2` would not let the ability initiate
        // against a hand with no candidate in it. The guard is on the card.
        var world = Deal();
        world.CreateCard(Hulk, Emptied(world, 0));
        var card = world.CreateCard(
            AuthoredCards.WhiteRabbit, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);

        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:enemy")]
    [Rule("rr:activation.1")]
    [Fact]
    public void SinisterOnslaughtSendsEveryCriminalAndNobodyElse()
    {
        // "**When Revealed (Hero):** Each [[Criminal]] enemy in play attacks
        // you." Rhino is Criminal and so is Sandman; Hydra Mercenary is Hydra,
        // and stays where it is. `rr:enemy` is why the villain is among them --
        // "an enemy is a minion or villain" -- and the trait is the whole of
        // the filter.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var criminal = world.CreateCard(
            Sandman, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(
            HydraMercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal(world, AuthoredCards.SinisterOnslaught);

        Assert.Equal(
            [villain.ObjectId, criminal.ObjectId],
            world.Agenda.Outstanding.Select(step => step.Subject));
        Assert.All(world.Agenda.Outstanding, step => Assert.Equal(Steps.Attack, step.What));
    }

    [Rule("rr:activation.1")]
    [Fact]
    public void AnAlterEgoIsSchemedAtRatherThanAttacked()
    {
        // The other half of the same sentence. `rr:activation.1` reads the form
        // to choose between attacking and scheming, and this card has printed
        // the choice out rather than leaving it to the activation -- so the
        // form is a test on the ability.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var criminal = world.CreateCard(
            Sandman, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Reveal(world, AuthoredCards.SinisterOnslaught);

        // The subjects as well as the verb: `Assert.All` over an empty agenda
        // passes, and an empty agenda is exactly what a broken query produces.
        Assert.Equal(
            [villain.ObjectId, criminal.ObjectId],
            world.Agenda.Outstanding.Select(step => step.Subject));
        Assert.All(world.Agenda.Outstanding, step => Assert.Equal(Steps.Scheme, step.What));
    }

    [Rule("rr:surge")]
    [Fact]
    public void WithNoCriminalInPlayTheCardSurgesInstead()
    {
        // "If no enemy attacked this way, this card gains surge." The same
        // query asked before the fact rather than a count taken after: every
        // enemy it names does activate, so an empty query and a count of zero
        // are the same board.
        //
        // Rhino carries the Criminal trait, so reaching this on the scenario
        // the set is dealt into means the villain is somehow not in play --
        // which is why the villain is moved away here rather than the board
        // being built from nothing.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        World.MoveToTop(
            world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.SinisterOnslaught);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:search.1")]
    [Rule("rr:when-revealed-abilities.2")]
    [Fact]
    public void CrimePaysPutsAMinionIntoPlayWithoutRevealingIt()
    {
        // "**When Revealed:** Search the encounter deck for a [[Criminal]]
        // minion and put it into play engaged with you."
        //
        // Put into play, not revealed, and `rr:when-revealed-abilities.2` is
        // the difference: "if an encounter card with a '**When Revealed**'
        // ability is put into play without being revealed, the '**When
        // Revealed**' ability does not trigger." Sandman is a Criminal minion
        // with no "When Revealed", so what this pins is that the card lands
        // engaged rather than being routed through the reveal step -- which
        // would run whatever text it had.
        var world = Deal();
        var deck = Emptied(world, DeckType.EncounterDeck);

        var sandman = world.CreateCard(Sandman, deck);

        Reveal(world, AuthoredCards.CrimePays);
        Run(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, sandman.Area.Type);
        Assert.Equal(PlayArea.Of(0), sandman.Area.PlayArea);
        Assert.True(sandman.FaceUp);
    }

    [Rule("rr:search")]
    [Fact]
    public void CrimePaysPassesOverAMinionWithoutTheTrait()
    {
        // "A **[[Criminal]]** minion." Hydra Mercenary is a minion and is
        // Hydra, so the search does not find it -- and the card's own sentence
        // for that case is the surge.
        var world = Deal();
        var deck = Emptied(world, DeckType.EncounterDeck);

        var hydra = world.CreateCard(HydraMercenary, deck);
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.CrimePays);
        Run(world);

        // Not engaged: the search passed over it, so nothing was put into
        // play. It did move -- `rr:surge.1` deals the player one facedown
        // encounter card and the Hydra Mercenary was the only card in the deck
        // to deal -- and being dealt facedown is not being put into play.
        Assert.NotEqual(DeckType.EngagedEnemiesArea, hydra.Area.Type);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:search")]
    [Fact]
    public void CrimePaysPassesOverACriminalThatIsNotAMinion()
    {
        // "A [[Criminal]] **minion**." The trait is half the criteria and the
        // card type is the other half -- Rhino's own card carries Criminal, and
        // a search that read only the trait would put the villain into a
        // player's play area as a minion.
        //
        // An artificial board: a villain card does not sit in the encounter
        // deck in a real game. What is under test is the interpreter's filter,
        // and the filter has to be shown both halves or one of them is
        // decoration.
        var world = Deal();
        var deck = Emptied(world, DeckType.EncounterDeck);
        var rhino = world.CreateCard("01094", deck);

        Reveal(world, AuthoredCards.CrimePays);
        Run(world);

        Assert.NotEqual(DeckType.EngagedEnemiesArea, rhino.Area.Type);
    }

    [Rule("rr:search.3")]
    [Fact]
    public void TheEncounterDeckIsShuffledWhetherOrNotTheSearchFoundAnything()
    {
        // "If **any portion** of a deck is searched, upon completion of that
        // game step, game function, or card ability, shuffle that entire deck."
        // Any portion, and upon completion -- so it happens on the branch that
        // found nothing too, and the card carries it in both.
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var before = deck.Cards.Select(card => card.ObjectId).ToList();

        Reveal(world, AuthoredCards.CrimePays);
        Run(world);

        // One card left the deck: `rr:surge.1` deals the player one, and the
        // Rhino encounter deck holds no Criminal minion for the search to find.
        Assert.NotEqual(
            before.Where(id => deck.Cards.Any(card => card.ObjectId == id)),
            deck.Cards.Select(card => card.ObjectId));
    }

    [Rule("rr:search.1")]
    [Fact]
    public void WithTwoCriminalMinionsThePlayerChoosesWhichOne()
    {
        // "If a player finds multiple cards that satisfy the criteria of a
        // search, **the player chooses** among those options." Not the
        // interpreter, and not the first one in the deck -- so the ability
        // stops and asks, and the question carries both.
        var world = Deal();
        var deck = Emptied(world, DeckType.EncounterDeck);

        var first = world.CreateCard(Sandman, deck);
        var second = world.CreateCard(Sandman, deck);

        Reveal(world, AuthoredCards.CrimePays);
        var asked = Sequence.Work(world, Cards, AuthoredCards.Runner(), []);

        Assert.NotNull(asked);
        Assert.Equal(Question.Element, asked!.Asking);
        Assert.Equal(
            [first.ObjectId, second.ObjectId],
            asked.Affordances.Select(offered => offered.Id));
    }

    private static void Reveal(World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, player);
    }

    [Rule("rr:attack-player-ability-type.step.7")]
    [Fact]
    public void ShockerStunsTheHeroThatAttackedIt()
    {
        // "**Forced Response:** After Shocker is attacked, stun the attacking
        // character." `rr:attack-player-ability-type.step.7` lists "after
        // [character] is attacked" among the forced abilities an attack's
        // resolution triggers -- and it is the reason a character's basic
        // attack is a step of the agenda rather than a call that returns.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        var shocker = world.CreateCard(
            AuthoredCards.SyndicateShocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, Cards, 0, shocker, []);
        Run(world);

        Assert.True(shocker.Damage > 0, "the attack landed");
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:ally.2")]
    [Rule("rr:you-your.15")]
    [Fact]
    public void AnAllyThatAttacksIsStunnedAndItsControllerIsNot()
    {
        // "The attacking **character**", not the attacking player.
        // `rr:ally.2` lets a player use an ally to attack, and
        // `rr:you-your.15` is emphatic that an ally's attack is **not**
        // performed by that player's identity -- so the hero standing behind
        // it is untouched.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        var cat = Ally(world, BlackCat, 0);
        var shocker = world.CreateCard(
            AuthoredCards.SyndicateShocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.AllyPower(world, Cards, cat, shocker, BasicPowers.AttackVerb, []);
        Run(world);

        Assert.True(Statuses.Has(world, cat, Statuses.Stunned));
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:attack-player-ability-type.step.9")]
    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void AnAllysConsequentialDamageComesAfterTheEnemyHasAnswered()
    {
        // `.step.9` puts consequential damage last, after the forced abilities
        // of `.step.7`. `rr:consequential-damage.1` says the same the other way
        // round -- "after resolving abilities that are triggered by the ally
        // attacking or thwarting".
        //
        // The order is observable because `rr:ally.3`'s parenthesis makes the
        // stun matter: "if an ally attempts to attack or thwart **while stunned
        // or confused**, respectively, that ally will not take consequential
        // damage." The ally here was not stunned when it attacked, so it takes
        // the damage; Shocker's stun landing first would not change that, and
        // what this pins is that both happened rather than one replacing the
        // other.
        // Spider-Woman rather than Black Cat: the consequential icons sit
        // under the field that was used, and Black Cat's star is on her THW.
        // An ally with no icon under its ATK takes nothing for attacking,
        // which would make this test pass without the step existing.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var woman = Ally(world, SpiderWoman, 0);
        var shocker = world.CreateCard(
            AuthoredCards.SyndicateShocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.AllyPower(world, Cards, woman, shocker, BasicPowers.AttackVerb, []);
        var happened = Run(world);

        Assert.True(Statuses.Has(world, woman, Statuses.Stunned));
        Assert.Equal(1, woman.Damage);

        // The order, which is the whole claim. Both happen either way; only
        // the sequence says whether the consequential damage was a step after
        // `.step.7` or a tail on the attack itself.
        var verbs = happened.Select(what => what.Verb).ToList();
        Assert.True(
            verbs.IndexOf("Give_Status") < verbs.IndexOf("Consequential_Damage"),
            $"the stun must come first; the events were {string.Join(", ", verbs)}");
    }

    [Rule("rr:dash-value.3")]
    [Fact]
    public void AsABoostCardShockerStunsTheStrongestCharacterYouControl()
    {
        // "★ **Boost:** Stun the character you control with the highest ATK
        // value." Spider-Man's ATK is 2 and Black Cat's is 1, so the hero is
        // the one that goes down.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        var cat = Ally(world, BlackCat, 0);
        var card = world.CreateCard(
            AuthoredCards.SyndicateShocker, world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().Boost(world, card, 0);
        Run(world);

        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.False(Statuses.Has(world, cat, Statuses.Stunned));
    }

    [Rule("rr:attack-player-ability-type.step.8")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnOptionalResponseToYourOwnAttackIsAskedAndCanBeTaken()
    {
        // `.step.8` is the other half of `.step.7`: "**non-forced** abilities
        // with the triggers listed above." A non-forced ability is a question,
        // and until this the player's own turn had nowhere to put one -- the
        // turn took an option, resolved it, and asked the turn prompt again, so
        // an ability waiting in a window was offered and then refused with
        // "taking that is not implemented".
        //
        // Written here rather than authored on a card, because no printed card
        // the engine reaches yet carries an optional response to an attack.
        // What is under test is the route, not a card.
        var book = AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "24045", "name": "test", "abilities": [ {
                "name": "test",
                "trigger": { "event": "WhenCharacterAttacks", "timing": "Response",
                             "subject": "this" },
                "effect": { "giveStatus": { "card": "attacker", "status": "confused" } }
            } ] } ] }
            """);

        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(AuthoredCards.SpiderMan);
        var shocker = world.CreateCard(
            AuthoredCards.SyndicateShocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var game = Game.Begin(world, Cards, new Marvel.Cards.Run.AbilityRunner(book));

        // Into the turn, then attack.
        game.Resolve(Decision.Decline);
        var attack = game.Pending!.Affordances.First(
            option => option.Verb == BasicPowers.AttackVerb
                && option.Targets!.Legal.Contains(shocker.ObjectId));
        game.Resolve(Decision.Take(attack.Id, [shocker.ObjectId], []));

        // The window opened during the player's own turn, and it is the
        // ability that is on offer rather than the turn's options.
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));

        Assert.True(Statuses.Has(world, identity, Statuses.Confused));

        // And the turn came back afterwards, because `rr:player-turn` lets
        // every option but changing form "be performed as many times as the
        // player is able".
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    /// <summary>Empties a deck to the removed area, and answers with it.</summary>
    private static Area Emptied(World world, DeckType type)
    {
        var deck = world.AreaOf(type);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        }

        return deck;
    }

    /// <summary>Empties a seat's opening hand, and answers with it.</summary>
    /// <remarks>
    /// A dealt hand is five cards nobody chose, so a test about which card an
    /// ability takes has to start from a hand it put there.
    /// </remarks>
    private static Area Emptied(World world, int seat)
    {
        var hand = world.Seats[seat].Hand;
        foreach (var held in hand.Cards.ToList())
        {
            Marvel.Rules.Play.Discard.Card(world, held, "test", []);
        }

        return hand;
    }

    private static Card Upgrade(World world, string faceId, int seat) =>
        world.CreateCard(
            faceId,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(seat), cardOwner: seat));

    private static Card Ally(World world, string faceId, int seat) =>
        world.CreateCard(
            faceId, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(seat), cardOwner: seat));

    /// <summary>
    /// Runs the agenda out. Declines what can be declined and takes the first
    /// answer to what cannot -- a card's own choice is not cancellable, because
    /// the ability is resolving and one of the things it offers will happen.
    /// </summary>
    private static List<GameEvent> Run(World world)
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

        return events;
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
