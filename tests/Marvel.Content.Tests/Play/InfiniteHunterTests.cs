using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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
/// Infinite Hunter — the first card that names the enemy whose activation it is
/// resolving inside.
/// </summary>
/// <remarks>
/// <para>
/// "<b>When Revealed:</b> Deal 3 damage to an ally you control. [star]
/// <b>Boost:</b> Choose to either place 2 threat on Gene Pool, or the
/// activating enemy gets +2 SCH and +2 ATK for this activation."
/// </para>
/// <para>
/// <b>The enemy is read off the board, not off the moment.</b> A boost card is
/// turned faceup in the middle of an activation, and its own occurrence is
/// about the boost card — so the card asking for "the activating enemy" has
/// nothing in the occurrence to answer with. <c>rr:activation</c> is what makes
/// one answer serve both kinds: "whenever an enemy attacks or schemes, it is
/// considered to have activated", and until now only the attack half had a
/// value on the board.
/// </para>
/// <para>
/// <c>TimingPoints.EndOfActivation</c> and not <c>EndOfAttack</c>:
/// <c>rr:activation.6</c> gives an activation an end, and a scheme is not an
/// attack. A +2 that outlived a scheme would go off during somebody's attack,
/// against somebody it was never about.
/// </para>
/// </remarks>
public sealed class InfiniteHunterTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;
    private const string BlackCat = "01002";

    /// <summary>An ally with four hit points, so three damage is readable.</summary>
    private const string WarMachine = "01030";

    /// <summary>The Age of Apocalypse's villainous minion, who gets boost cards.</summary>
    private const string Ozymandias = "45159";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void TheRevealAsksWhichAllyAndDealsItThreeDamage()
    {
        // War Machine has four hit points, so three damage is a number that
        // stays on the board to be read. An ally with two would be defeated by
        // two damage or by three alike, and the amount would go untested.
        var (world, runner) = Board();
        var ally = world.CreateCard(
            WarMachine, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var card = world.CreateCard(
            AuthoredCards.InfiniteHunter, world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, card, 0);

        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, card, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal(Question.Element, asked.Asking);
        Assert.Equal([ally.ObjectId], asked.Affordances.Select(option => option.Id));

        runner.Chose(world, card, 0, waiting.Index, Decision.Take(ally.ObjectId), waiting.Tier);

        Assert.Equal(3, ally.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
    }

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void TheRevealChoosesOneAllyAndLeavesTheOthersAlone()
    {
        // "**An** ally", singular, and `rr:choose-game-element.1` makes the
        // choice the resolving player's. Two allies is what tells "choose one"
        // apart from "each" — the card damages one of them and the other is
        // untouched, whichever way the answer went.
        var (world, runner) = Board();
        var chosen = world.CreateCard(
            WarMachine, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var spared = world.CreateCard(
            WarMachine, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var card = world.CreateCard(
            AuthoredCards.InfiniteHunter, world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, card, 0);

        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, card, 0, waiting.Index, Decision.Take(chosen.ObjectId), waiting.Tier);

        Assert.Equal(3, chosen.Damage);
        Assert.Equal(0, spared.Damage);
    }

    [Rule("rr:choose-game-element")]
    [Fact]
    public void TheRevealWithNoAllyAsksNothingAndDoesNothing()
    {
        // Guarded by `exists`, because there is nothing to choose from and the
        // card names no alternative -- unlike Caught Off Guard, whose surge
        // sits in the branch that would have got here.
        var (world, runner) = Board();
        var card = world.CreateCard(AuthoredCards.InfiniteHunter, world.AreaOf(DeckType.RevealingArea));

        Assert.Empty(runner.WhenRevealed(world, card, 0));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void TheBoostOffersTheTwoOptionsTheCardPrints()
    {
        var (world, runner) = Board();
        var unus = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(unus.ObjectId, Player: 0, Attacking: false);
        var card = world.CreateCard(AuthoredCards.InfiniteHunter, world.AreaOf(DeckType.RevealingArea));

        runner.Boost(world, card, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, card, 0, waiting.Index, waiting.Tier)!;

        Assert.Equal(Question.Option, asked.Asking);
        Assert.False(asked.Cancellable);
        Assert.Equal(["placeThreat", "seq"], asked.Affordances.Select(option => option.Label));
    }

    [Rule("rr:attack-enemy-activation.step.3.a")]
    [Rule("rr:attack-enemy-activation.step.3.e")]
    [Fact]
    public void ABoostChoiceFinishesBeforeTheNextBoostCardTurnsFaceup()
    {
        var (world, runner) = Board();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var second = deck.Cards[^1];
        var hunter = world.CreateCard(AuthoredCards.InfiniteHunter, deck);
        World.MoveToTop(hunter, deck);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));

        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.GiveAdditionalBoostCard(world, villain, "test", events);
        while (defend.Asking != Question.Defender)
        {
            Sequence.Answer(
                world, Cards, runner, defend,
                defend.Cancellable
                    ? Decision.Decline
                    : Decision.Take(Assert.Single(defend.Affordances).Id),
                events);
            defend = Sequence.Work(world, Cards, runner, events)!;
        }
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var choose = Sequence.Work(world, Cards, runner, events)!;

        Assert.Equal(Question.Option, choose.Asking);
        Assert.Equal(DeckType.BoostingArea, hunter.Area.Type);
        Assert.True(hunter.FaceUp);
        Assert.Equal(DeckType.BoostCardsDeck, second.Area.Type);
        Assert.False(second.FaceUp);

        Sequence.Answer(world, Cards, runner, choose, Decision.Take(1), events);
        var asked = Sequence.Work(world, Cards, runner, events);
        while (asked is not null)
        {
            Sequence.Answer(
                world, Cards, runner, asked,
                asked.Cancellable
                    ? Decision.Decline
                    : Decision.Take(Assert.Single(asked.Affordances).Id),
                events);
            asked = Sequence.Work(world, Cards, runner, events);
        }

        Assert.Equal(DeckType.EncounterDiscardPile, hunter.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
    }

    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:response")]
    [Fact]
    public void SuspendedBoostFinishesBeforeTheFlipResponseWindow()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            {"cards":[
              {"card":"45065","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
                "effect":{"choose":{"options":[
                  {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}},
                  {"grantUntil":{"card":"activatingEnemy","keyword":"attack",
                                 "amount":2,"until":"EndOfActivation"}}
                ]}}
              }]},
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenBoostCardsFlipped","timing":"ForcedResponse",
                           "subject":"game"},
                "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
              }]}
            ]}
            """));
        var (world, _) = Board();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        world.CreateCard(
            "01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var hunter = world.CreateCard(AuthoredCards.InfiniteHunter, deck);
        World.MoveToTop(hunter, deck);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));

        var defend = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var choose = Sequence.Work(world, Cards, runner, events)!;

        Assert.Equal(Question.Option, choose.Asking);
        Assert.Equal(before, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.BoostingArea, hunter.Area.Type);

        Sequence.Answer(world, Cards, runner, choose, Decision.Take(1), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));

        Assert.Equal(before + 1, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EncounterDiscardPile, hunter.Area.Type);
    }

    [Rule("rr:activation")]
    [Rule("rr:alteration-effect")]
    [Theory]
    // A scheme activation: `rr:scheme-enemy-activation.step.3` reads the
    // modified SCH, and Unus's first stage prints 1.
    [InlineData(false, 3)]
    // An attack activation: `rr:attack-enemy-activation.step.4` reads the
    // modified ATK, and Unus's first stage prints 2.
    [InlineData(true, 4)]
    public void TheBuffReachesTheEnemyWhoseActivationItIs(bool attacking, long expected)
    {
        var (world, runner, unus) = Activating(attacking);
        Assert.Equal(expected, Measure(world, runner, unus, attacking, buff: true));
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void TakingTheOtherOptionFeedsGenePoolAndBuffsNobody()
    {
        // The branch not taken is the assertion that matters: an interpreter
        // running both options would pass every test above.
        var (world, runner, unus) = Activating(attacking: false);
        var pool = world.Cards.First(card => card.FaceId == AuthoredCards.GenePool);
        long before = pool.Tokens.GetValueOrDefault("k_threat");

        // Unus's first stage prints SCH 1, and nothing adds to it.
        Assert.Equal(1, Measure(world, runner, unus, attacking: false, buff: false));
        Assert.Equal(before + 2, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:villainous")]
    [Fact]
    public void TheBuffGoesToTheMinionActivatingAndNotToTheVillain()
    {
        // "The **activating** enemy", which is not "the villain".
        // `rr:attack-enemy-activation.step.1` gives a boost card to "a villain,
        // or a minion with the villainous keyword", so a villainous minion can
        // turn this card faceup during its own attack — and the +2 is its.
        //
        // Ozymandias (`45159`) is the Age of Apocalypse's own villainous
        // minion, printing ATK 1 against Unus's 2, so the two cannot be
        // confused for one another by the number that comes out.
        var (world, runner) = Board();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);

        var minion = world.CreateCard(
            Ozymandias, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        long dealt = Activation(world, runner, minion, attacking: true, buff: true);

        // ATK 1, plus the two the boost card gave it. Unus prints ATK 2 and is
        // standing right there.
        Assert.Equal(3, dealt);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void TheBuffIsGoneOnceTheActivationHasEnded()
    {
        // "That activation **ends immediately** and no further steps of that
        // activation resolve." A +2 that outlived a scheme would go off during
        // the next thing the villain does.
        var (world, runner, unus) = Activating(attacking: false);
        Measure(world, runner, unus, attacking: false, buff: true);

        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "scheme");
        Assert.Null(world.Activation);
    }

    [Rule("rr:activation")]
    [Fact]
    public void ABoostCardOutsideAnActivationHasNoEnemyToName()
    {
        // The board is where the answer lives, so there has to be one. A boost
        // card is only ever turned faceup inside an activation; if one is not,
        // the card says so rather than reaching for the villain.
        var (world, runner) = Board();
        var card = world.CreateCard(AuthoredCards.InfiniteHunter, world.AreaOf(DeckType.RevealingArea));

        runner.Boost(world, card, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
            world, card, 0, waiting.Index, Decision.Take(1), waiting.Tier));

        Assert.Contains("no enemy is activating", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:lasting-effects")]
    [Fact]
    public void AGrantForADurationIsHeldAgainstTheFieldsTheEngineReads()
    {
        // "+2 SCH" is the same mechanism as "gains overkill" and reaches it
        // through the same door, so a grant is held against the fields
        // modifiers are actually read into. Unchecked, a name nobody
        // recognises registers happily, expires on time, and modifies nothing
        // in between -- a typo that looks like a working card.
        var (world, _) = Board();
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "45065", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Boost", "subject": "this" },
                "effect": { "grantUntil": { "card": { "query": "villain" },
                                            "keyword": "scheeme", "amount": 2,
                                            "until": "EndOfActivation" } }
            } ] } ] }
            """));

        var card = world.CreateCard("45065", world.AreaOf(DeckType.RevealingArea));

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => runner.Boost(world, card, 0));

        Assert.Contains("grants 'scheeme'", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>The dealt Unus board.</summary>
    private static (World World, AbilityRunner Runner) Board()
    {
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            runner);

        return (world, runner);
    }

    /// <summary>The same board, with Unus about to attack or scheme.</summary>
    private static (World World, AbilityRunner Runner, Card Unus) Activating(bool attacking)
    {
        var (world, runner) = Board();
        var unus = world.TheCardIn(DeckType.VillainArea)!;

        if (attacking)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        return (world, runner, unus);
    }

    /// <summary>
    /// How much one activation by Unus is worth, with the boost card's second
    /// option taken.
    /// </summary>
    private static long Measure(
        World world, AbilityRunner runner, Card unus, bool attacking, bool buff) =>
        Activation(world, runner, unus, attacking, buff);

    /// <summary>
    /// How much one activation is worth, with the boost card's second option
    /// taken.
    /// </summary>
    private static long Activation(
        World world, AbilityRunner runner, Card enemy, bool attacking, bool buff)
    {
        var identity = world.Seats[0].IdentityCard;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = attacking
            ? identity.Damage
            : main.Tokens.GetValueOrDefault("k_threat");

        // On top of the encounter deck, so `rr:attack-enemy-activation.step.1`
        // and `rr:scheme-enemy-activation.step.1` deal it as the boost card.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var hunter = world.CreateCard(AuthoredCards.InfiniteHunter, deck);
        World.MoveToTop(hunter, deck);

        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(
            attacking ? Steps.Attack : Steps.Scheme,
            1, 2, Index: 0, Subject: enemy.ObjectId, Seat: 0));

        var asked = Sequence.Work(world, Cards, runner, events);
        while (asked is not null)
        {
            var input = asked.Asking switch
            {
                Question.Option => Decision.Take(buff ? 1 : 0),
                Question.Defender => Decision.Decline,
                _ => Decision.Decline,
            };

            Sequence.Answer(world, Cards, runner, asked, input, events);
            asked = Sequence.Work(world, Cards, runner, events);
        }

        return (attacking ? identity.Damage : main.Tokens.GetValueOrDefault("k_threat"))
            - before;
    }
}
