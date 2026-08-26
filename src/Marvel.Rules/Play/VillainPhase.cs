using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// What a card does when it is revealed from the encounter deck.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam where rules stop and cards begin.</b> Everything in
/// <see cref="VillainPhase"/> is the Rules Reference — threat, activation,
/// boost, dealing and revealing — and none of it needs to know what any
/// particular card says. This is the one thing it does need, and it is
/// deliberately an interface so the card DSL interpreter can satisfy it without
/// the villain phase changing.
/// </para>
/// <para>
/// <c>docs/card-dsl.md</c> designs that interpreter and opens with "nothing here
/// is implemented". Until it exists, <c>Marvel.Content</c> supplies the handful
/// of cards a recorded transition actually reaches, and this interface is what
/// stops that being a parallel path: there is one place a card's behaviour can
/// enter the engine, and the interpreter replaces what is behind it.
/// </para>
/// </remarks>
public interface ICardAbilities : IWindowAbilities
{
    /// <summary>Resolves a revealed encounter card's "When Revealed" ability.</summary>
    /// <param name="world">The world.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat it was dealt to.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player);

    /// <summary>
    /// Resolves a faceup boost card's "Boost" ability —
    /// <c>rr:boost-boost-icon.2</c>.
    /// </summary>
    /// <remarks>
    /// Step 2b of both activations, and its own ability type
    /// (<c>AbilityType.Boost</c>) rather than a window: <c>rr:ability</c> puts
    /// it at the occurrence tier, so there is nothing to offer and nothing to
    /// decline. The card is in the boosting area while this runs and is
    /// discarded by step 2d afterwards.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">The boost card, faceup.</param>
    /// <param name="player">The seat the activation is against.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> Boost(World world, Card card, int player);

    /// <summary>
    /// Resolves a defeated card's "When Defeated" abilities —
    /// <c>rr:when-defeated-abilities</c>.
    /// </summary>
    /// <remarks>
    /// <c>.2.1</c> puts it before the card goes: "a defeated card leaves play
    /// <b>after</b> its When Defeated ability is resolved, if any." So the
    /// card is still where it was while this runs, which is what lets the
    /// ability read its own tokens and what is attached to it.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">The card that was defeated.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> WhenDefeated(World world, Card card);

    /// <summary>
    /// Step 1 of dealing damage — <c>rr:damage.step.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Abilities that trigger <i>when [character] would be dealt any amount of
    /// damage</i>", which is where a replacement effect sits:
    /// <c>rr:replacement-effect</c> — "when [triggering condition] would happen,
    /// do [replacement effect] instead".
    /// </para>
    /// <para>
    /// It answers with how much damage is <b>left</b>, because that is what the
    /// rest of the steps act on. A card that replaces all of it answers zero,
    /// and <c>rr:replacement-effect.1</c> is then satisfied for free: the
    /// damage is no longer imminent, so nothing later in the order can respond
    /// to it.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="target">Who the damage is aimed at.</param>
    /// <param name="amount">How much is about to be dealt.</param>
    /// <param name="events">Where to record what any replacement did.</param>
    /// <returns>How much damage is still to be dealt.</returns>
    long WouldBeDealt(World world, Card target, long amount, List<GameEvent> events);

    /// <summary>
    /// The "<b>Resource</b>" abilities a player could generate from —
    /// <c>rr:resource-ability</c>.
    /// </summary>
    /// <remarks>
    /// "A resource ability can be triggered <b>anytime the player who controls
    /// the ability is generating resources to pay a cost</b>", so these sit
    /// beside the cards in hand rather than in a window: they are another way
    /// to make a resource, not another moment.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="player">Whose cost is being paid.</param>
    /// <returns>One source per ability still available this round.</returns>
    IReadOnlyList<Prompts.ResourceSource> ResourceAbilities(World world, int player);

    /// <summary>
    /// Uses one resource ability to help pay a cost, and answers what it made.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ResourceAbilities"/> because generating is a
    /// choice: the affordance offers the ability and the answer takes it, the
    /// same shape a card in hand has. <c>rr:limit</c> counts the use here.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="player">Whose cost is being paid.</param>
    /// <param name="card">The card whose ability it is.</param>
    /// <returns>The resource letters it generated.</returns>
    string UseResource(World world, int player, int card);

    /// <summary>
    /// The "<b>Action</b>" abilities one player may trigger on their turn —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// Not a window. An action is one of the six things a turn offers, so it is
    /// asked with the others rather than in an interrupt or a response — which
    /// is why it is here and not in <see cref="IWindowAbilities.Waiting"/>.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="player">Whose turn it is.</param>
    /// <returns>What they could trigger, which may be nothing.</returns>
    IReadOnlyList<PendingAbility> Actions(World world, int player);

    /// <summary>
    /// Triggers an "Action", paying its cost —
    /// <c>rr:initiating-abilities.step.5</c> and <c>.step.6</c>.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="IWindowAbilities.Resolve"/> because an action
    /// is answered rather than merely taken: a cost of spending resources is a
    /// choice of <i>which</i> cards, and the answer carries it.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="ability">Which action, from <see cref="Actions"/>.</param>
    /// <param name="paying">Cards discarded to pay, by object id.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying);

    /// <summary>
    /// The question a suspended ability is waiting on —
    /// <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:choose-game-element.1</c> settles who is asked, and it is the
    /// player resolving the ability rather than the first player or the card's
    /// owner. An encounter card has no owner, so there would be nobody else to
    /// ask.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="source">The card whose ability is waiting.</param>
    /// <param name="player">The seat resolving it.</param>
    /// <param name="stoppedAt">
    /// Where the ability stopped — the step of its top-level sequence to pick
    /// up at. An ability can ask more than once, so <i>which</i> choice is
    /// waiting is part of the question.
    /// </param>
    /// <returns>The question, or null when there is nothing to ask.</returns>
    Prompts.Prompt? Choosing(World world, Card source, int player, int stoppedAt);

    /// <summary>
    /// Resolves a card's "<b>Setup</b>" abilities —
    /// <c>rr:setup-triggered-ability</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "'<b>Setup</b>' is a type of triggered ability that is resolved during
    /// setup", and <c>.1</c> makes it mandatory — so there is nothing to offer
    /// and nothing to decline, which is why this resolves rather than opening a
    /// window.
    /// </para>
    /// <para>
    /// <b>Which step of setup is the caller's business.</b>
    /// <c>rr:setup-triggered-ability.2</c> puts an encounter card's at
    /// "Resolve Scenario Setup and When Revealed Abilities"
    /// (<c>rr:appendix-ii-setup.step.12</c>) and <c>.3</c> puts a player card's
    /// at a later step of its own. The two are far apart — the opening hands
    /// are drawn between them — so this takes the card and the deal decides
    /// when.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">The card whose setup ability, if any, is due.</param>
    /// <returns>What changed, which is nothing for most cards.</returns>
    IReadOnlyList<GameEvent> Setup(World world, Card card);

    /// <summary>
    /// The effects this card's constant abilities have in force right now —
    /// <c>rr:ability.5</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Asked, never told.</b> A constant ability "becomes active as soon as
    /// its card enters play and remains active while the card is in play", and
    /// <c>rr:ability.9</c> adds that one seeking a condition is "active anytime
    /// the specific condition is met" — so what it is doing is a function of
    /// the board and not an event anybody has to remember to fire. Unus gains
    /// retaliate the moment the ninth threat lands on Gene Pool and loses it
    /// the moment the scheme is thwarted, with nothing in between to notice.
    /// </para>
    /// <para>
    /// Which is why this returns rather than registers. Registration would need
    /// an enter-play hook on every path a card can take into play, and a missed
    /// path is an ability that is silently never in force — the failure this
    /// engine throws rather than allow. <see cref="Timing.ContinuousEffects"/>
    /// calls this while answering what is in force, so there is one path and it
    /// is the board itself.
    /// </para>
    /// <para>
    /// <b>Pure.</b> It is asked many times per game state and may be asked
    /// while other rules are mid-flight, so it must not deal damage, move
    /// cards, or record events.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">A card in play.</param>
    /// <returns>What its constant abilities are doing, which may be nothing.</returns>
    IReadOnlyList<Timing.ContinuousEffect> Constant(World world, Card card);

    /// <summary>Resolves the option that answer names.</summary>
    /// <param name="world">The world.</param>
    /// <param name="source">The card whose ability is waiting.</param>
    /// <param name="player">The seat resolving it.</param>
    /// <param name="stoppedAt">Where the ability stopped.</param>
    /// <param name="input">Which option they took.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input);


}

/// <summary>Nothing has an ability. What an engine with no cards ported does.</summary>
/// <remarks>
/// Open rather than sealed, and every member virtual, so that something which
/// does <i>one</i> thing can say only that. Tests want a card that answers a
/// window and nothing else far more often than they want the whole interface,
/// and nine copies of "return an empty list" is nine places for this interface
/// to grow through.
/// </remarks>
public class NoCardAbilities : ICardAbilities
{
    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Boost(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenDefeated(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual long WouldBeDealt(
        World world, Card target, long amount, List<GameEvent> events) => amount;

    /// <inheritdoc/>
    public virtual IReadOnlyList<Prompts.ResourceSource> ResourceAbilities(
        World world, int player) => [];

    /// <inheritdoc/>
    public virtual string UseResource(World world, int player, int card) =>
        throw new RulesNotImplementedException(
            "no card has a resource ability, so none of them can be used");

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Actions(World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying) =>
        throw new RulesNotImplementedException(
            "no card has an action, so none of them can be triggered");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Setup(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<Timing.ContinuousEffect> Constant(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input) =>
        throw new RulesNotImplementedException(
            "no card has an ability, so none of them is waiting on a choice");

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be resolved from one");

    /// <inheritdoc/>
    public virtual Prompts.Affordance Describe(World world, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be described from one");
}

/// <summary>
/// The villain phase, step by step, as <c>rr:villain-phase</c> lists them.
/// </summary>
/// <remarks>
/// <para>
/// The steps are numbered here as the Rules Reference numbers them, so a
/// divergence can be argued against the published text rather than against this
/// file. What is implemented is what the recorded milestone game reaches; the
/// rest throws rather than silently doing nothing, because a villain phase that
/// quietly skipped minion activation would produce a plausible board that is
/// wrong.
/// </para>
/// <para>
/// <b>The order is the whole thing.</b> The boost card is drawn before the
/// encounter card and discarded before it, which is why the recorded discard
/// pile holds the boost card at index 0 and the encounter card at index 1. Draw
/// them the other way round and every subsequent card in the encounter deck
/// shifts.
/// </para>
/// </remarks>
public static class VillainPhase
{
    /// <summary>Schedule the villain phase's six steps.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:villain-phase</c> lists six, and they are six values here rather
    /// than the order of six method calls. That is not tidiness: a window may
    /// hold an ability somebody has to be asked about, and a phase that is a
    /// call has nowhere to stop. See <see cref="Agenda"/>.
    /// </para>
    /// <para>
    /// Steps 2 and 4 are headings rather than occurrences, so they open no
    /// windows of their own; what happens under them — one activation, one card
    /// revealed — is scheduled when they are reached.
    /// </para>
    /// </remarks>
    /// <param name="agenda">What the game still has to do.</param>
    /// <param name="round">Which round this is.</param>
    public static void Schedule(Agenda agenda, int round)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        agenda.Add(new PhaseStep(Steps.PlaceThreat, round, 1));
        agenda.Add(new PhaseStep(Steps.EnemiesActivate, round, 2, Plan: true));
        agenda.Add(new PhaseStep(Steps.DealEncounterCards, round, 3));
        agenda.Add(new PhaseStep(Steps.RevealEncounterCards, round, 4, Plan: true));
        agenda.Add(new PhaseStep(Steps.PassFirstPlayerToken, round, 5));
        agenda.Add(new PhaseStep(Steps.EndVillainPhase, round, 6));
    }

    /// <summary>Take one step of the villain phase.</summary>
    /// <remarks>
    /// Returns a prompt when the step itself has something to ask, which one of
    /// them does: <c>rr:attack-enemy-activation.step.2</c> asks whether anybody
    /// defends. That is not a window — nobody is using an ability — so it is the
    /// step that stops, and the answer comes back to
    /// <see cref="Answered"/>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">Which step.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The question the step is waiting on, or null.</returns>
    /// <exception cref="RulesNotImplementedException">
    /// The board reached a rule this engine does not have — a minion engaged
    /// with a player, or an attack that would defeat its target.
    /// </exception>
    public static Prompt? Take(
        World world, ICardFacts facts, ICardAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        switch (step.What)
        {
            case Steps.PlaceThreat:
                PlaceThreat(world, facts, abilities, events);
                break;

            case Steps.EnemiesActivate:
                PlanActivations(world, facts, step);
                break;

            case Steps.Scheme:
                Scheme(world, facts, abilities, world.Cards[step.Subject], step.Seat, events);
                break;

            case Steps.Attack:
                Attack.Initiate(world, facts, step, events);
                break;

            case Steps.GiveBoostCard:
                Attack.GiveBoostCard(world, facts, events);
                break;

            case Steps.DeclareDefender:
                return Attack.DeclareDefender(world, facts);

            case Steps.FlipBoostCards:
                Attack.FlipBoostCards(world, facts, abilities, events);
                break;

            case Steps.DealAttackDamage:
                Attack.DealDamage(world, facts, events);
                break;

            case Steps.CharacterAttacks:
                BasicPowers.ResolveCharacterAttack(world, facts, events);
                break;

            case Steps.AllyConsequentialDamage:
                BasicPowers.Consequential(
                    world, facts, world.Cards[step.Subject], byAttack: true,
                    BasicPowers.AttackVerb, events);
                break;

            case Steps.EndAttack:
                Attack.End(world, events);
                break;

            case Steps.DealEncounterCards:
                DealEncounterCards(world, facts, events);
                break;

            case Steps.RevealEncounterCards:
                RevealNextEncounterCard(world, step);
                break;

            case Steps.RevealEncounterCard:
                RevealEncounterCard(
                    world, facts, abilities, world.Cards[step.Subject], step.Seat,
                    step.Round, events);
                break;

            case Steps.ChooseOption:
                return abilities.Choosing(
                    world, world.Cards[step.Subject], step.Seat, step.Index);

            case Steps.PassFirstPlayerToken:
                PassFirstPlayerToken(world);
                break;

            case Steps.EndVillainPhase:
                PhaseEnd.EndVillainPhase(world, facts, events);
                break;

            case Steps.DrawToHandSize:
                PhaseEnd.DrawToHandSize(world, facts, events);
                break;

            case Steps.ReadyCards:
                PhaseEnd.ReadyCards(world, events);
                break;

            case Steps.EndPlayerPhase:
                PhaseEnd.EndPlayerPhase(world, events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"the villain phase has no step '{step.What}'");
        }

        return null;
    }

    /// <summary>Give a step the answer it stopped for.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step that asked.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Answered(
        World world, ICardFacts facts, ICardAbilities abilities, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        switch (step.What)
        {
            case Steps.DeclareDefender:
                Attack.Defend(world, facts, input, events);
                break;

            case Steps.ChooseOption:
                events.AddRange(abilities.Chose(
                    world, world.Cards[step.Subject], step.Seat, step.Index, input));
                break;

            default:
                throw new RulesNotImplementedException(
                    $"step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    /// <summary>
    /// Step 2, as one activation per player — <c>rr:villain-phase.step.2</c>,
    /// "in player order, each player resolves".
    /// </summary>
    private static void PlanActivations(World world, ICardFacts facts, PhaseStep step)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return;
        }

        foreach (int seat in world.PlayerOrder)
        {
            // `rr:activation.1`: hero form and the enemy attacks, alter-ego
            // form and it schemes. Which face is showing *is* which form, so
            // this needs no separate flag.
            var identity = world.Seats[seat].IdentityCard;
            bool attacking = facts.Kind(identity.FaceId) != CardKind.AlterEgo;

            // `rr:villain-phase.step.2.a` then `.step.2.b`: "the villain
            // activates against the player", and then "each minion engaged with
            // the player activates against them, in the order of that player's
            // choice".
            //
            // **The order is the player's and this takes it in the order they
            // sit in the play area.** `rr:minion.3` says so outright, and the
            // recorded prompt vocabulary has a verb for asking --
            // `Minion_Activates_Order`, twice in the fixture. Asking is not
            // implemented; the order here is deterministic and stated rather
            // than a silent pick.
            var enemies = new List<int> { villain.ObjectId };
            enemies.AddRange(world
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
                .Cards
                .Select(minion => minion.ObjectId));

            foreach (int enemy in enemies)
            {
                world.Agenda.Then(new PhaseStep(
                    attacking ? Steps.Attack : Steps.Scheme,
                    step.Round, 2, Index: seat, Subject: enemy, Seat: seat));
            }
        }
    }

    /// <summary>Step 1. Threat from the main scheme's acceleration field.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.1</c>: "Place the amount of threat indicated in the
    /// main scheme's acceleration field onto that scheme." The engine's name for
    /// that field is <c>EscalationThreat</c>, and it is per-player —
    /// <c>1*</c> on <c>01097b</c>, so one threat at one player and three at
    /// three. Acceleration icons and tokens add more; nothing on the milestone
    /// board has one.
    /// </remarks>
    private static void PlaceThreat(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea);
        if (scheme is null)
        {
            return;
        }

        // `rr:villain-phase.step.1`: "place the amount of threat indicated in
        // the main scheme's acceleration field. **If any acceleration icons or
        // tokens are active, additional threat equal to the number of such
        // icons and tokens is also placed at this time.**"
        long amount = facts.PrintedValue(scheme.FaceId, "EscalationThreat", world.Players)
            + MainScheme.Acceleration(world, facts);
        Threat.Place(
            world, facts, abilities, scheme, amount, "villain phase, place threat", events);
    }

    /// <summary>An enemy schemes. <c>rr:scheme-enemy-activation</c>.</summary>
    /// <remarks>
    /// Three steps: give it one facedown boost card from the encounter deck,
    /// resolve that card (flip, add its boost icons to SCH, discard), then place
    /// threat equal to the modified SCH on the main scheme.
    /// </remarks>
    private static void Scheme(
        World world, ICardFacts facts, ICardAbilities abilities, Card villain, int seat,
        List<GameEvent> events)
    {
        // `rr:activation.6`: "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve." A scheme is an activation -- `rr:activation`
        // says so -- and a minion can be defeated between being scheduled to
        // scheme and getting to. `rr:in-play-and-out-of-play.2` is what in play
        // means for an encounter card.
        if (!DeckTypes.IsInPlay(villain.Area.Type))
        {
            return;
        }

        // `rr:confuse-confused.1`: "when this character would scheme or thwart,
        // remove each confused status card from it instead." The scheme does
        // not happen, so no boost card is given and no threat is placed.
        if (BasicPowers.Cancelled(world, facts, villain, Statuses.Confused, events))
        {
            return;
        }

        long scheme = facts.PrintedValue(villain.FaceId, "SCH", world.Players);

        // `rr:scheme-enemy-activation.step.1`, the same clause the attack has:
        // a villain always, a minion only with `rr:villainous`.
        if (Keywords.IsBoosted(villain, facts, world.Players))
        {
            scheme += ResolveBoostCard(world, facts, abilities, seat, events);
        }

        var target = world.TheCardIn(DeckType.MainSchemesArea);
        if (target is not null)
        {
            Threat.Place(world, facts, abilities, target, scheme, "scheme", events);
        }

        // The other kind of activation ends here. A boost card's ability that
        // says "this activation" was given for *this* scheme and must not
        // survive into somebody's attack -- `rr:activation` makes a scheme an
        // activation, and `rr:activation.6` gives an activation an end.
        world.Effects.Expire(TimingPoints.EndOfActivation);
    }

    /// <summary>Gives the enemy a boost card, resolves it, and returns its icons.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:boost-boost-icon.1</c>: a star icon is not a boost icon and adds
    /// nothing. The printed <c>Boost</c> attribute is the icon count already —
    /// <c>01186</c> Advance has none and <c>01101</c> Hydra Mercenary has one —
    /// so a star card and a zero-boost card are the same number here and differ
    /// only in having an ability, which the interpreter will run.
    /// </para>
    /// <para>
    /// <c>rr:boost-boost-icon.5</c>: discard it after applying. The boost card
    /// is drawn and discarded <b>before</b> the encounter cards are dealt, which
    /// is why the recorded discard pile has it underneath.
    /// </para>
    /// </remarks>
    private static long ResolveBoostCard(
        World world, ICardFacts facts, ICardAbilities abilities, int seat,
        List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = EncounterDeck.TakeTop(world, "boost", events);
        if (boost is null)
        {
            return 0;
        }

        // Through the boosting area and not straight to the discard. No
        // recorded step catches a card in transit -- the whole activation
        // happens between two decisions -- but passing through is what
        // registers the card's token pools, and the discarded card's
        // `k_threat` key is on the wire. See `DeckTypes.GrantsTokenPool`.
        var boosting = world.AreaOf(DeckType.BoostingArea);
        boosting.Append(boost);

        // `rr:scheme-enemy-activation.step.2.b` -- "resolve any **Boost**
        // abilities, indicated by the star icon in the boost area", and it is
        // step 2b: while the card is faceup in the boosting area, before 2d
        // discards it.
        events.AddRange(abilities.Boost(world, boost, seat));

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        World.MoveToTop(boost, discard);

        events.Add(new CardsMoved(
            Places.Reference(deck),
            Places.Reference(discard),
            [new Landing(boost.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Boost",
        });

        // `rr:amplify-icon`, the same clause on the scheming side: a boost card
        // turned faceup during an enemy activation gains one icon per amplify
        // icon in play.
        return facts.PrintedValue(boost.FaceId, "Boost", world.Players)
            + MainScheme.Amplify(world, facts);
    }

    /// <summary>Step 3. One encounter card to each player, in player order.</summary>
    /// <remarks>
    /// Hazard icons deal additional cards. Nothing on the milestone board has
    /// one, and a board that did would deal too few here — so it throws.
    /// </remarks>
    /// <summary>Step 3. One card each, plus one per hazard icon in play.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.step.3</c>: "Deal one encounter card to each player.
    /// Deal one additional card for each hazard icon on a card in play. These
    /// additional cards are dealt in player order."
    /// <para>
    /// Nothing here schedules a reveal. Step 4 drains the queue instead, which
    /// is what lets a card dealt at any other moment — by an ability, or by a
    /// player's deck running out mid-turn — be revealed in the same step as the
    /// rest.
    /// </para>
    /// </remarks>
    private static void DealEncounterCards(
        World world, ICardFacts facts, List<GameEvent> events)
    {
        foreach (int seat in world.PlayerOrder)
        {
            if (Deal.EncounterCard(world, seat, "villain phase", events) is null)
            {
                return;
            }
        }

        // `rr:hazard-icon`: "for each hazard icon on cards in play, deal one
        // player one additional card *(not one card per player)*. Additional
        // cards are dealt in player order" -- so these go round the table one
        // at a time, wrapping, rather than one round per icon.
        long icons = Deal.HazardIcons(world, facts);
        for (long dealt = 0; dealt < icons; dealt++)
        {
            int seat = (world.FirstPlayer + (int)(dealt % world.Players)) % world.Players;
            if (Deal.EncounterCard(world, seat, "hazard", events) is null)
            {
                return;
            }
        }
    }

    /// <summary>Step 4, one card at a time, until the queue is empty.</summary>
    private static void RevealNextEncounterCard(World world, PhaseStep step)
    {
        if (Deal.NextToReveal(world) is not { } next)
        {
            return;
        }

        // The reveal is an occurrence with its own windows; this heading is
        // not. Scheduling itself *after* the reveal is what makes step 4 a
        // loop -- a card revealed here can deal another, and `rr:deal.1` puts
        // that one in the same step.
        //
        // **The order of these two calls is the loop's termination.**
        // `Agenda.Then` appends in call order, so the reveal has to be
        // scheduled first; the other way round, this heading runs again with
        // the card still in the queue and schedules itself forever.
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard, step.Round, 4,
            Index: step.Index, Subject: next.Card.ObjectId, Seat: next.Player));
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCards, step.Round, 4, Index: step.Index + 1, Plan: true));
    }

    /// <summary>Step 4. Each player reveals their cards, in the order dealt.</summary>
    private static void RevealEncounterCard(
        World world, ICardFacts facts, ICardAbilities abilities, Card card, int player,
        int round, List<GameEvent> events)
    {
        // `rr:reveal.4.1` -- "if the card specifies a player to give it to,
        // **that player is considered to be revealing it**." One reassignment
        // and not a special case at the placement, because being the revealing
        // player is the whole of what the rule says: `rr:obligation.1` makes
        // every "you" on the card point at the player whose area it is in, and
        // `rr:obligation.4` puts it in the named player's.
        //
        // At one player the named player and the revealing player are the same
        // seat, which is why this went unnoticed. Above one they are not.
        switch (Reveal.Names(world, facts, card))
        {
            case null:
                break;

            case >= 0 and var named:
                player = named;
                break;

            default:
                // `rr:obligation.5` -- "if an obligation cannot be given to the
                // specified player for any reason, **ignore the card's
                // ability, remove it from the game, and reveal an additional
                // encounter card**." Dealt rather than revealed directly: step
                // 4 is a loop over what a player has been dealt, so a card put
                // in that queue is revealed by the same step -- which is how
                // `rr:surge` already works.
                var gone = world.AreaOf(DeckType.RemovedArea);
                var was = card.Area;
                World.MoveToTop(card, gone);
                events.Add(new CardsMoved(
                    Places.Reference(was), Places.Reference(gone),
                    [new Landing(card.ObjectId, gone.Cards.Count - 1)])
                {
                    Trigger = "villain phase", Verb = "Remove",
                });

                Deal.EncounterCard(world, player, "obligation", events);
                return;
        }

        // Same reason as the boost card: the revealing area is where an
        // encounter card registers its pools.
        World.MoveToTop(card, world.AreaOf(DeckType.RevealingArea));
        card.TurnFaceUp();
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = "villain phase", Verb = "Reveal",
        });

        // `rr:reveal.step.2` -- **where the card goes is decided by its type**,
        // and it happens before step 3's "When Revealed" abilities. A minion
        // that entered play is already engaged when its own ability resolves.
        Reveal.Resolve(world, facts, card, player, events);

        // Step 3. "Resolve each **When Revealed** ability on that card
        // *(including those provided by keywords)*."
        //
        // **The order between them is the first player's choice and this does
        // not ask.** `rr:forced.5`: "if two or more forced abilities would
        // initiate at the same moment, the first player determines the order in
        // which the abilities initiate" -- and a card carrying surge and its own
        // When Revealed text has exactly two. The prompt is not implemented, so
        // the order here is fixed and deterministic rather than chosen. See
        // MARVEL-187.
        Reveal.Keywords(world, facts, abilities, card, player, events);
        events.AddRange(abilities.WhenRevealed(world, card, player));

        // `rr:quickstrike.2` puts this after the card's own abilities, and it
        // is the one keyword that does something *after* them rather than
        // beside them.
        Reveal.Quickstrike(world, facts, card, player, round);

        // `rr:teamwork.2` puts this in the same place, after the card's own
        // abilities. A minion carrying both keywords activates twice, which is
        // what two forced responses to one moment do -- `rr:forced.5` gives the
        // first player their order, and asking is MARVEL-187.
        Reveal.Teamwork(world, facts, card, player, round);

        // Step 4. "If the card is a treachery, discard it." Asked as "is it
        // still where step 2 left something not in play", so that an ability
        // that put the card somewhere is not undone.
        if (card.Area.Type != DeckType.RevealingArea)
        {
            return;
        }

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var from = card.Area;
        World.MoveToTop(card, discard);
        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Reveal",
        });
    }

    /// <summary>Step 5. <c>rr:villain-phase.step.5</c>, to the next clockwise player.</summary>
    private static void PassFirstPlayerToken(World world) =>
        world.FirstPlayer = world.Players > 0 ? (world.FirstPlayer + 1) % world.Players : 0;

}
