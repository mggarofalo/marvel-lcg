using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Ported cards waiting in a window, on the real Rhino board.
/// </summary>
/// <remarks>
/// <para>
/// The timing spine was built and cited before anything used it, and until now
/// nothing did: the engine had no card whose ability could wait in one. These
/// are the tests that make it load-bearing — and every card in them is a row in
/// <c>datasets/abilities/abilities.json</c> rather than a line of C#.
/// </para>
/// <para>
/// <b>Two cards, one window, and that is the point.</b> Rhino attacking is a
/// single occurrence with two abilities waiting in its interrupt window —
/// Charge's <b>Forced Interrupt</b> at tier 2b and Spider-Man's <b>Interrupt</b>
/// at 2c. <c>rr:attack-enemy-activation.5</c> is the rule that puts them
/// together: an interrupt triggering "when [enemy name] attacks" has the same
/// timing as one triggering "when the villain initiates an attack".
/// </para>
/// <para>
/// <b>Why the recording cannot reach this.</b> The sampling policy declines
/// every decision, so its hero never leaves alter-ego form, and
/// <c>rr:activation.1</c> makes a villain facing an alter-ego scheme rather than
/// attack. The board here is the real dealt one with two changes stated in the
/// setup: the identity is turned to its hero face, and Charge is put into play
/// by its own ported "Attach to Rhino".
/// </para>
/// </remarks>
public sealed class CardsInWindowsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;
    private static readonly string[] Heroes = ["spider_man"];

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attack-enemy-activation.5")]
    [Rule("rr:forced.4")]
    [Fact]
    public void RhinoAttackingResolvesChargeAndThenOffersSpiderSense()
    {
        // "Forced interrupts take priority and initiate before non-forced
        // interrupts." Charge is forced and resolves without anybody being
        // asked; Spider-Sense is optional and reaches the player as a question.
        var (world, abilities, _) = Attacking();
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, Cards, abilities, events);

        Assert.NotNull(asked);
        Assert.Equal(Question.Opportunity, asked.Asking);
        Assert.Equal(TimingPriority.Interrupt, asked.When);
        Assert.Equal(0, asked.Player);
        Assert.True(asked.Cancellable);
        Assert.Contains("Enemy attack · Initiation · Interrupt window", asked.Description);
        Assert.Contains("Rhino is initiating an attack against Spider-Man", asked.Description);
        Assert.Contains("Target: Spider-Man", asked.Description);
        Assert.Equal(["Spider-Sense"], asked.Affordances.Select(a => a.Label));
        Assert.Equal(["Spider-Sense"], asked.Affordances.Select(a => a.Verb));

        // Charge fired on the way past, and what it did is a lasting effect
        // rather than anything the board shows at this moment.
        Assert.Contains(
            world.Effects.Active(),
            effect => effect.Kind == Keywords.Overkill);
    }

    [Rule("rr:status-cards.2")]
    [Rule("rr:stun-stunned.1")]
    [Fact]
    public void WebbedUpReplacesTheAttackAndLeavesItsStunForTheNextAttack()
    {
        // faq:01009 says Webbed Up prevents the attached enemy's next attack,
        // then its stun prevents that enemy's following attack. Because the
        // first attack never initiates, Spider-Sense is not offered for it.
        var board = Attacking();
        var world = board.World;
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(board.Charge, world.AreaOf(DeckType.EncounterDiscardPile));
        var webbed = world.CreateCard(AuthoredCards.WebbedUp, world.Seats[0].Hand);
        World.MoveToTop(
            webbed,
            world.AreaOf(DeckType.UpgradesArea, rhino.Area.PlayArea, rhino.ObjectId));
        int hand = world.Seats[0].Hand.Cards.Count;

        var asked = Sequence.Work(world, Cards, board.Abilities, []);

        Assert.Null(asked);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, webbed.Area.Type);
        Assert.True(Statuses.Has(world, rhino, Statuses.Stunned));
        Assert.Null(world.Attack);
    }

    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void ChargeFiresOnceAndNotOncePerVisitToTheWindow()
    {
        // "Each interrupt can only be triggered once per occurrence of the
        // triggering condition." Using an ability does not close the window --
        // `rr:interrupt.5` is about *further* abilities -- so the window is
        // re-entered with the board changed, and an occurrence that forgot what
        // had fired would grant overkill a second time.
        var (world, abilities, _) = Attacking();
        var identity = world.Seats[0].IdentityCard;
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, Cards, abilities, events);
        Sequence.Answer(
            world, Cards, abilities, asked!, Decision.Take(identity.ObjectId), events);
        Sequence.Work(world, Cards, abilities, events);

        Assert.Single(world.Effects.Active(), effect => effect.Kind == Keywords.Overkill);
    }

    [Rule("rr:attack-enemy-activation.1.4")]
    [Fact]
    public void SpiderSenseAnswersAnAttackOnSpiderManAndNotOnSomebodyElse()
    {
        // "When the villain initiates an attack against *you*." Abilities that
        // trigger this way "are resolved when/after a player is attacked", and
        // the player is the one the attack was initiated against -- so the
        // villain turning on the next seat is not Spider-Man's to interrupt.
        //
        // Two seats, because at one the question does not arise: every attack
        // is against you when you are the only player there is.
        var world = Deal("spider_man", "she_hulk");
        var abilities = AuthoredCards.Runner();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        var spiderMan = world.Seats[0].IdentityCard;

        var mine = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            rhino.ObjectId, spiderMan.ObjectId, player: 0);
        var theirs = Occurrence.ForAttack(
            2, [Steps.AttackInitiated], world, Cards,
            rhino.ObjectId, world.Seats[1].IdentityCard.ObjectId, player: 1);

        Assert.Contains(
            abilities.Waiting(world, mine, WindowKind.Interrupt),
            ability => ability.Type == AbilityType.Interrupt);
        Assert.DoesNotContain(
            abilities.Waiting(world, theirs, WindowKind.Interrupt),
            ability => ability.Type == AbilityType.Interrupt);
    }

    [Rule("rr:star-icon.2")]
    [Fact]
    public void ChargeAnswersRhinoAttackingAndNotSomeOtherEnemy()
    {
        // "When *Rhino* attacks", and the star that says so is in Charge's own
        // ATK field: `rr:star-icon.2` makes it a reminder "to check that
        // attachment's text box whenever *the attached enemy* uses the value
        // that field is modifying to attack". A minion attacking is not the
        // attached enemy attacking, and Charge adds nothing to its ATK.
        var (world, abilities, _) = Attacking();
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        int target = world.Seats[0].IdentityCard.ObjectId;
        var byRhino = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards, rhino.ObjectId, target, player: 0);
        var byMinion = Occurrence.ForAttack(
            2, [Steps.AttackInitiated], world, Cards, minion.ObjectId, target, player: 0);

        Assert.Contains(
            abilities.Waiting(world, byRhino, WindowKind.Interrupt),
            ability => ability.Type == AbilityType.ForcedInterrupt);
        Assert.DoesNotContain(
            abilities.Waiting(world, byMinion, WindowKind.Interrupt),
            ability => ability.Type == AbilityType.ForcedInterrupt);
    }

    [Rule("rr:ability.1")]
    [Rule("rr:in-play-and-out-of-play")]
    [Rule("rr:in-play-and-out-of-play.10")]
    [Fact]
    public void ChargeWaitsInTheWindowOnlyWhileItIsInPlay()
    {
        // A card's ability functions while the card is in play, and being
        // attached to Rhino is not the same as being in play: the recorded
        // Tough hangs off Rhino from a zone that is not. A second copy put
        // there is still hosted and still does nothing.
        var (world, abilities, _) = Attacking();
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        var spare = world.Cards.Last(card => card.FaceId == AuthoredCards.Charge);
        World.MoveToTop(
            spare,
            world.AreaOf(DeckType.StatusArea, rhino.Area.PlayArea, rhino.ObjectId));
        spare.TurnFaceDown();
        Assert.False(spare.FaceUp, "a facedown card attached to an in-play card is out of play");

        var attacking = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            rhino.ObjectId, world.Seats[0].IdentityCard.ObjectId, player: 0);

        Assert.Single(
            abilities.Waiting(world, attacking, WindowKind.Interrupt),
            ability => ability.Type == AbilityType.ForcedInterrupt);
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void TakingSpiderSenseDrawsACardAndDecliningDoesNot()
    {
        // "Unless prefaced by the word 'Forced', all interrupt and response
        // abilities are optional", so both answers are real and they differ.
        Assert.Equal(1, HandGrowth(Take: true));
        Assert.Equal(0, HandGrowth(Take: false));
    }

    [Rule("rr:attack-enemy-activation.step.2")]
    [Rule("rr:defend-defense.2")]
    [Fact]
    public void TheAttackThenAsksWhetherSpiderManDefends()
    {
        // Step 2 of the attack, and a question the rules give a step of its
        // own. Declining is an answer -- `rr:attack-enemy-activation.4`, an
        // attack nobody defends is undefended and still resolves.
        var (world, abilities, _) = Attacking();
        var asked = Answer(world, abilities, Decision.Decline);

        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);
        Assert.Equal(0, asked.Player);
        Assert.True(asked.Cancellable);
        Assert.Equal([Attack.DefenseVerb], asked.Affordances.Select(a => a.Verb));
    }

    [Rule("rr:attack-enemy-activation.step.2")]
    [Fact]
    public void AnsweringTheDefenceQuestionIsWhatMakesThatStepHappen()
    {
        // A step that asks is not finished until it is answered, and then it is
        // finished: the agenda moves on to step 3 rather than putting the same
        // question again.
        var (world, abilities, _) = Attacking();

        // The first question is Spider-Sense's window; the second is the
        // defence.
        Answer(world, abilities, Decision.Decline);
        Answer(world, abilities, Decision.Decline);

        Assert.DoesNotContain(
            Steps.DeclareDefender,
            world.Agenda.Outstanding.Select(step => step.What));
    }

    [Rule("rr:attack-enemy-activation.step.4")]
    [Theory]
    // Rhino stage 1 (01094) prints ATK 2, Charge attached adds its printed
    // `ATK+ 3`, and round one's boost card is worth nothing -- the same
    // zero-icon card the recorded game draws. Undefended, all five land.
    [InlineData(false, 5)]
    // "If a hero has been declared the defender of the attack, reduce the
    // amount of damage dealt by that hero's DEF value." Spider-Man's DEF is 3.
    [InlineData(true, 2)]
    public void DefendingReducesTheDamageByTheHerosDefence(bool defend, int expected)
    {
        var (world, abilities, _) = Attacking();
        var identity = world.Seats[0].IdentityCard;

        var defender = Answer(world, abilities, Decision.Decline);
        Assert.Equal(Question.Defender, defender!.Asking);

        Prompt? damageInterrupt = Answer(world, abilities,
            defend ? Decision.Take(identity.ObjectId) : Decision.Decline);

        Assert.NotNull(damageInterrupt!.Description);
        Assert.Contains($"for {expected} damage", damageInterrupt.Description);
        Assert.Contains("Spider-Man", damageInterrupt.Description);
        Assert.Contains("Charge", damageInterrupt.Description);

        // Backflip is now a real optional interrupt in the imminent-damage
        // window. This test is about ordinary defended damage, so decline it.
        Answer(world, abilities, Decision.Decline);

        Assert.Equal(expected, identity.Damage);
        Assert.Equal(!defend, identity.Ready);
    }

    [Rule("rr:delayed-effect.1")]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:alteration-effect")]
    [Fact]
    public void AtTheEndOfTheAttackChargeDiscardsItselfAndOverkillExpires()
    {
        // "At the end of this attack, discard Charge." Both halves of the card
        // are bounded by the attack: the keyword it granted expires because its
        // duration names that timing point, and the discard resolves because it
        // was waiting on that condition.
        var (world, abilities, charge) = Attacking();
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            // Bounded: a step that asks the same question forever is the
            // failure this is most likely to meet, and a hang says less than a
            // failure does.
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked after 10 answers");
            Sequence.Answer(world, Cards, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }

        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Attack);
        Assert.Equal(DeckType.EncounterDiscardPile, charge.Area.Type);
        Assert.Empty(world.Effects.Active());
        Assert.Contains(events, e => e is CardDetached detached && detached.Card == charge.ObjectId);
    }

    [Rule("rr:ability.step.2.c")]
    [Rule("rr:forced.4")]
    [Fact]
    public void TheWholeGameStopsInTheVillainPhaseToAskAboutSpiderSense()
    {
        // The same thing again through the engine's own entry point rather than
        // through `Sequence`, because that is what a client sees: the mulligan,
        // the turn, the end of the phase, and the game is part-way through the
        // villain phase holding a question a card put there.
        //
        // The last of the three is not a decline. This board is in hero form,
        // so its hand size is Spider-Man's 5 against the 6 Peter Parker was
        // dealt, and `rr:end-of-player-phase.step.1` makes discarding the odd
        // one out compulsory.
        var game = Playing();

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var result = EndPhase(game);

        Assert.Equal(GamePhase.VillainPhase, game.Phase);
        Assert.NotNull(result.Prompt);
        Assert.Equal(Question.Opportunity, result.Prompt.Asking);
        Assert.Equal(["Spider-Sense"], result.Prompt.Affordances.Select(a => a.Label));
        // "Forced interrupts take priority and initiate before non-forced
        // interrupts." The public game loop must apply Charge before offering
        // Spider-Sense, just as the narrower agenda test requires.
        Assert.Equal(TimingPriority.Interrupt, result.Prompt.When);
        Assert.True(result.Prompt.Cancellable);
        Assert.Contains(game.State.Effects.Active(), effect => effect.Kind == Keywords.Overkill);
        Assert.True(game.State.Windows.IsResolving);
    }

    /// <summary>How many cards the hand gains from answering the window.</summary>
    private static int HandGrowth(bool Take)
    {
        var (world, abilities, _) = Attacking();
        var identity = world.Seats[0].IdentityCard;
        int before = world.Seats[0].Hand.Cards.Count;

        Answer(world, abilities, Take ? Decision.Take(identity.ObjectId) : Decision.Decline);

        return world.Seats[0].Hand.Cards.Count - before;
    }

    /// <summary>Walks to the next question, answers it, and walks on.</summary>
    private static Prompt? Answer(World world, ICardAbilities abilities, Decision input)
    {
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events)
                    ?? throw new InvalidOperationException("nothing was asked");
        Sequence.Answer(world, Cards, abilities, asked, input, events);
        return Sequence.Work(world, Cards, abilities, events);
    }

    /// <summary>A board part-way through being set up for an attack.</summary>
    private sealed record Board(World World, AbilityRunner Abilities, Card Charge);

    /// <summary>A dealt board with Rhino about to attack a hero-form Spider-Man.</summary>
    private static Board Attacking()
    {
        var board = Prepare(Deal());
        var rhino = board.World.TheCardIn(DeckType.VillainArea)!;

        // Only the attack, so the encounter cards after it are not dealt: what
        // this file is about is the window, and the rest of the phase is
        // `PlayerPhaseTests`' business.
        board.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: 0, Subject: rhino.ObjectId, Seat: 0));
        return board;
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:alteration-effect")]
    [Fact]
    public void ACardGivingAStatusGoesThroughTheRulesRatherThanRoundThem()
    {
        // `"I'm Tough"` gives Rhino a tough status card. A second copy gives
        // him nothing: `rr:status-cards.1` caps every type at one, and a card
        // ability reaching straight past that cap would be a second place for
        // the status rules to live.
        var world = Deal();
        var abilities = AuthoredCards.Runner();
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        var copies = world.Cards.Where(card => card.FaceId == AuthoredCards.ImTough).ToList();

        abilities.WhenRevealed(world, copies[0], 0);
        Assert.Equal(1, Statuses.Count(world, rhino, Statuses.Tough));

        int queued = world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;
        abilities.WhenRevealed(world, copies[1], 0);
        Assert.Equal(1, Statuses.Count(world, rhino, Statuses.Tough));
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    /// <summary>Answers <c>rr:end-of-player-phase.step.1</c>, discarding the excess.</summary>
    private static Resolution EndPhase(Game game)
    {
        var world = game.State;
        var seat = world.Seats[game.Pending!.Player];
        long limit = PhaseEnd.HandSize(world, seat, Cards);
        var excess = seat.Hand.Cards
            .Take(Math.Max(0, seat.Hand.Cards.Count - (int)limit))
            .Select(card => card.ObjectId)
            .ToArray();

        var affordance = game.Pending.Affordances.Single(a => a.Verb == Game.EndPhaseVerb);
        return game.Resolve(Decision.Take(affordance.Id, excess, []));
    }

    /// <summary>The same board, driven through the engine from the mulligan.</summary>
    private static Game Playing()
    {
        var board = Prepare(Deal());
        return Game.Begin(board.World, Cards, board.Abilities);
    }

    private static World Deal(params string[] heroes)
    {
        var playing = heroes.Length > 0 ? heroes : Heroes;
        return WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }

    /// <summary>Hero form, and Charge in play.</summary>
    private static Board Prepare(World world)
    {
        var abilities = AuthoredCards.Runner();

        // `rr:activation.1` -- the villain attacks an identity in hero form and
        // schemes against one in alter-ego form. Turning the card is the whole
        // of changing form; there is no separate flag.
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);

        // Put into play by the card's own ported text, "Attach to Rhino",
        // rather than by placing it on the table here. The Rhino set holds more
        // than one copy, so this is the first of them and not the only one.
        //
        // Through the reveal, because `rr:attach-to` makes the phrase a rule
        // about a card entering play rather than a "When Revealed" ability —
        // so the route into play is what resolves it, and calling the ability
        // directly would now attach nothing.
        var charge = world.Cards.First(card => card.FaceId == AuthoredCards.Charge);
        world.Abilities = abilities;
        Reveal.Resolve(world, Cards, charge, 0, []);

        return new Board(world, abilities, charge);
    }
}
