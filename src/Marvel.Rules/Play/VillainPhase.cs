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
    /// <summary>Applies card text that determines the state a card enters play with.</summary>
    /// <remarks>
    /// Some cards print a constant such as "enters play with 4 arrow counters"
    /// without using the Uses keyword. The rules layer owns the transition and
    /// asks the card layer for that printed state before any response to the
    /// transition is offered. The default is silence for cards with no such
    /// text.
    /// </remarks>
    IReadOnlyList<GameEvent> EntersPlay(World world, Card card) => [];

    /// <summary>
    /// Resumes card effects that initiated an activation after it has fully resolved.
    /// </summary>
    /// <remarks>
    /// <c>rr:activation.7</c>: an effect that initiates an activation is
    /// considered resolved only after that activation has fully resolved. The
    /// result names that activation rather than whichever activation happened
    /// most recently.
    /// </remarks>
    IReadOnlyList<GameEvent> ActivationCompleted(
        World world, EnemyActivation result) => [];

    /// <summary>
    /// Whether threat may currently be removed from a scheme.
    /// </summary>
    /// <remarks>
    /// A constant prohibition is a question the rules layer must ask before
    /// either a basic thwart or a card effect removes tokens. The default is
    /// the engine's choice for an ability source that has no such prohibition;
    /// a card interpreter overrides it when a card in play says otherwise.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="scheme">The scheme threat would be removed from.</param>
    /// <returns>Whether the removal is permitted.</returns>
    bool CanRemoveThreat(World world, Card scheme) => true;

    /// <summary>
    /// The resources a card in hand generates toward the current payment.
    /// </summary>
    /// <remarks>
    /// The rules specify what each card generates; they do not specify an
    /// engine API. The engine chooses to pass the card being paid for because
    /// a generator may make a different amount for particular cards.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="source">The card being spent from hand.</param>
    /// <param name="payingFor">The card being paid for, or null for an ability cost.</param>
    /// <returns>The resource letters generated.</returns>
    string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
        Resources.GeneratedBy(source.FaceId, world.Facts);

    /// <summary>Applies card-specific restrictions to an attack's defenders.</summary>
    /// <param name="world">The board.</param>
    /// <param name="attack">The attack whose defender is being declared.</param>
    /// <param name="candidates">Every character the defense rules permit.</param>
    /// <returns>The candidates the card permits and whether one is required.</returns>
    DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates) =>
        new(candidates, Required: false);

    /// <summary>Resolves a revealed encounter card's "When Revealed" ability.</summary>
    /// <param name="world">The world.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat it was dealt to.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player);

    /// <summary>Resolves a reveal inside its saveable agenda occurrence.</summary>
    /// <remarks>
    /// The overload is the engine's persistence boundary for
    /// <c>rr:resolve.2-.8</c>. Existing rules-only implementations may keep the
    /// three-argument method; the card interpreter uses the occurrence to
    /// retain ability and treachery status through suspension and responses.
    /// </remarks>
    IReadOnlyList<GameEvent> WhenRevealed(
        World world, Card card, int player, Occurrence occurrence) =>
        WhenRevealed(world, card, player);

    /// <summary>Cancels every When Revealed ability before any one of them applies.</summary>
    bool CancelWhenRevealed(
        World world, Card card, int player, Occurrence occurrence) => false;

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
    /// Resolves the abilities that trigger when a card is defeated —
    /// <c>rr:damage.step.7</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One timing point, two kinds of card.</b> Step 7 of <c>rr:damage</c>
    /// is "abilities that trigger <i>when [character] is defeated…</i>
    /// <i>(including <b>When Defeated</b> abilities)</i>", and the parenthesis
    /// is the whole of it: the defeated card's own ability and another card's
    /// forced interrupt on the same defeat are the same moment. Genetic
    /// Experiments — "<b>Forced Interrupt</b>: When attached minion is
    /// defeated, place 2 threat on Gene Pool" — is the second kind, and
    /// <c>rr:when-defeated-abilities.1</c> makes the first kind
    /// "<b>Forced Interrupt</b>: When this card is defeated…" in so many
    /// words.
    /// </para>
    /// <para>
    /// It sits <i>after</i> the damage, which is why it is a call rather than
    /// a window. <c>rr:damage.step.5</c> places the damage and
    /// <c>.step.8</c> discards the defeated card; step 7 is between them, and
    /// the occurrence's interrupt window closed before step 1. Nothing is lost
    /// by that for a <i>forced</i> ability — <c>rr:forced.1</c> leaves nothing
    /// to offer and nothing to decline. A non-forced interrupt on another
    /// card's defeat would need a window that can ask, and is refused by name
    /// rather than skipped.
    /// </para>
    /// <para>
    /// <c>rr:when-defeated-abilities.2.1</c> puts all of it before the card
    /// goes: "a defeated card leaves play <b>after</b> its When Defeated
    /// ability is resolved, if any." So the card is still where it was while
    /// this runs, which is what lets an ability read its own tokens and what
    /// is attached to it — and what lets an attachment still be in play to
    /// answer.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">The card that was defeated.</param>
    /// <param name="defeated">
    /// Who did it and how. An ability may need it —
    /// "<b>When Defeated</b>: the player who defeated this scheme confuses
    /// their identity" — and it is not readable from the board, because the
    /// board records what a card <i>is</i> and not what happened to it.
    /// </param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated);

    /// <summary>Resolves the body of a card ability labelled as an attack.</summary>
    void ResolveCardAttack(
        World world, CharacterAttack attack, Timing.Occurrence occurrence,
        List<GameEvent> events);

    /// <summary>Resolves the body of a card ability labelled as a thwart.</summary>
    void ResolveCardThwart(
        World world, CharacterThwart thwart, Timing.Occurrence occurrence,
        List<GameEvent> events);

    /// <summary>Whether the target can take damage from this source.</summary>
    /// <remarks>
    /// <c>rr:cannot</c>: "cannot" is absolute. This is a constant prohibition,
    /// not a triggered replacement effect in the damage window.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="target">Who would take the damage.</param>
    /// <param name="source">The card the damage comes from.</param>
    bool CanTakeDamage(World world, Card target, Card source);

    /// <summary>
    /// Step 1 of dealing damage — <c>rr:damage.step.1</c>.
    /// </summary>
    /// <remarks>
    /// "Abilities that trigger <i>when [character] would be dealt any amount of
    /// damage</i>", which is where a replacement effect sits. It answers with
    /// how much damage is left for the rest of the sequence.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="target">Who the damage is aimed at.</param>
    /// <param name="source">The card dealing the damage.</param>
    /// <param name="amount">How much is about to be dealt.</param>
    /// <param name="events">Where to record what any replacement did.</param>
    /// <returns>How much damage is still to be dealt.</returns>
    long WouldBeDealt(
        World world, Card target, Card source, long amount, List<GameEvent> events);

    /// <summary>
    /// Step 6 of dealing damage — <c>rr:damage.step.6</c>.
    /// </summary>
    /// <remarks>
    /// The damage is already on the character, so an ability can replace the
    /// imminent defeat by changing the board. The caller checks the
    /// character's remaining hit points again after this returns.
    /// </remarks>
    void WouldBeDefeated(World world, Card target, List<GameEvent> events);

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
    /// <param name="events">Where to record costs paid to use it.</param>
    /// <returns>The resource letters it generated.</returns>
    string UseResource(World world, int player, int card, List<GameEvent> events);

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
    /// <param name="chosen">
    /// The objects chosen for it, in the order they were chosen. A cost can ask
    /// for one — Hunted's "discard a card from your hand" names no resource and
    /// no type, only a card — and <c>rr:initiating-abilities</c> keeps choosing
    /// and paying in different steps, so the two lists arrive separately.
    /// </param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> Act(
        World world,
        PendingAbility ability,
        IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen);

    /// <summary>Resolves an accepted Action inside its live agenda occurrence.</summary>
    IReadOnlyList<GameEvent> Act(
        World world,
        PendingAbility ability,
        IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        Occurrence occurrence) => Act(world, ability, paying, chosen);

    /// <summary>
    /// Resolves the <b>Special</b> ability on a card named by another ability —
    /// <c>rr:special</c>.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="card">The card carrying the Special ability.</param>
    /// <param name="player">The player resolving it.</param>
    /// <param name="finalStep">Whether it is the final step of its parent sequence.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> ResolveSpecial(
        World world, Card card, int player, bool finalStep) =>
        throw new RulesNotImplementedException(
            $"card '{card.FaceId}' has no implemented Special ability");

    /// <summary>Resolves one persisted frame of an "each player" effect.</summary>
    /// <remarks>
    /// Rules owns the chosen order and the saveable frame. The card interpreter
    /// owns the effect tree and reconstructs it from the source, exact ability,
    /// and structural path each time a frame runs. A fresh call per seat is
    /// what keeps form-dependent choices and effect-local results isolated.
    /// </remarks>
    IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool finalPlayer) =>
        throw new RulesNotImplementedException(
            $"card '{source.FaceId}' has no implemented each-player continuation");

    /// <summary>Resume card text after its persisted enemy-activation wait.</summary>
    IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation) =>
        throw new RulesNotImplementedException(
            $"card ability {continuation.AbilityOrdinal} has no implemented activation continuation");

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
    /// Legacy top-level resume index. New continuations also carry the exact
    /// ability ordinal and structural path on their agenda step.
    /// </param>
    /// <param name="tier">
    /// Which of the card's abilities stopped, or null when the card has only
    /// one ability with a choice in it and there is nothing to tell apart. A
    /// card can have a choice in more than one, and the card and the position
    /// do not say which.
    /// </param>
    /// <returns>The question, or null when there is nothing to ask.</returns>
    Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, Timing.AbilityType? tier = null);

    /// <summary>
    /// The question a suspended ability is waiting on, with its parent
    /// sequence position preserved.
    /// </summary>
    /// <remarks>
    /// The default keeps existing interpreters source-compatible. An
    /// interpreter whose effect reads the flag implements this overload and
    /// carries it into its resumed resolution.
    /// </remarks>
    Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep) =>
        Choosing(world, source, player, stoppedAt, tier);

    /// <summary>
    /// Reconstructs a choice with its each-player continuation context.
    /// </summary>
    Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <summary>
    /// The game element this card's "attach to" phrase names —
    /// <c>rr:attach-to</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If a card uses the phrase 'attach to', it must be attached to
    /// <i>(placed beneath and slightly overlapped by)</i> the specified game
    /// element <b>as it enters play</b>." A rule about the phrase, not an
    /// ability — so the engine attaches on every path into play and the card
    /// supplies only the element, which is all this answers.
    /// </para>
    /// <para>
    /// <c>rr:attach-to.3</c> is why it may answer nothing: "the 'attach to'
    /// phrase is checked for legality when the card would be attached [...] if
    /// the initial check does not pass, the card is not able to be attached, so
    /// it remains in its prior state or game area." Null is that check failing
    /// as well as the card printing no such phrase, and the caller treats them
    /// alike because the rule does.
    /// </para>
    /// <para>
    /// <b>Pure</b>, like <see cref="Constant"/>: it is asked while a card is
    /// being placed, so it must not move anything itself.
    /// </para>
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="card">The card entering play.</param>
    /// <returns>The object id it attaches to, or null.</returns>
    int? AttachesTo(World world, Card card);

    /// <summary>Legal hosts a player may choose while playing an attachment.</summary>
    IReadOnlyList<int>? AttachmentTargets(World world, Card card);

    /// <summary>
    /// The in-play player cards whose <b>Setup</b> abilities are due at setup
    /// step 16.
    /// </summary>
    /// <remarks>
    /// The rules layer cannot infer this from printed words, so the card layer
    /// identifies the cards and the game orders them by player and object id.
    /// Returning cards rather than resolving them here lets the ordinary agenda
    /// suspend one ability for a choice before the next Setup ability begins.
    /// </remarks>
    /// <param name="world">The board after opening hands and mulligans.</param>
    /// <param name="player">The player whose cards are being considered.</param>
    /// <returns>Setup-bearing cards controlled by that player and in play.</returns>
    IReadOnlyList<Card> PlayerSetupCards(World world, int player) => [];

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
    /// <param name="tier">Which of the card's abilities stopped.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier = null);

    /// <summary>
    /// Resolves an answered ability choice with its parent sequence position
    /// preserved.
    /// </summary>
    IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep) =>
        Chose(world, source, player, stoppedAt, input, tier);

    /// <summary>
    /// Resumes a choice with the persisted each-player frame it belongs to.
    /// </summary>
    IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep);

    /// <summary>Resumes a choice with its persisted event-stream provenance.</summary>
    IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep, eachPlayerFrame, finalPlayer);


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
    public virtual IReadOnlyList<GameEvent> EntersPlay(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ActivationCompleted(
        World world, EnemyActivation result) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResumeAbility(
        World world, PhaseStep continuation) => [];

    /// <inheritdoc/>
    public virtual bool CanRemoveThreat(World world, Card scheme) => true;

    /// <inheritdoc/>
    public virtual string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
        Resources.GeneratedBy(source.FaceId, world.Facts);

    /// <inheritdoc/>
    public virtual DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates) =>
        new(candidates, Required: false);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Boost(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenCardDefeated(
        World world, Card card, Defeated defeated) => [];

    /// <inheritdoc/>
    public virtual void ResolveCardAttack(
        World world, CharacterAttack attack, Timing.Occurrence occurrence,
        List<GameEvent> events) =>
        throw new RulesNotImplementedException("no card attack effect is registered");

    /// <inheritdoc/>
    public virtual void ResolveCardThwart(
        World world, CharacterThwart thwart, Timing.Occurrence occurrence,
        List<GameEvent> events) =>
        throw new RulesNotImplementedException("no card thwart effect is registered");

    /// <inheritdoc/>
    public virtual bool CanTakeDamage(World world, Card target, Card source) => true;

    /// <inheritdoc/>
    public virtual long WouldBeDealt(
        World world, Card target, Card source, long amount, List<GameEvent> events) => amount;

    /// <inheritdoc/>
    public virtual void WouldBeDefeated(
        World world, Card target, List<GameEvent> events)
    {
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<Prompts.ResourceSource> ResourceAbilities(
        World world, int player) => [];

    /// <inheritdoc/>
    public virtual string UseResource(
        World world, int player, int card, List<GameEvent> events) =>
        throw new RulesNotImplementedException(
            "no card has a resource ability, so none of them can be used");

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Actions(World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen) =>
        throw new RulesNotImplementedException(
            "no card has an action, so none of them can be triggered");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResolveSpecial(
        World world, Card card, int player, bool finalStep) =>
        throw new RulesNotImplementedException(
            $"card '{card.FaceId}' has no implemented Special ability");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool finalPlayer) =>
        throw new RulesNotImplementedException(
            $"card '{source.FaceId}' has no implemented each-player continuation");

    /// <inheritdoc/>
    public virtual int? AttachesTo(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<int>? AttachmentTargets(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<Card> PlayerSetupCards(World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Setup(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<Timing.ContinuousEffect> Constant(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier = null) => null;

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep) =>
        Choosing(world, source, player, stoppedAt, tier);

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier = null) =>
        throw new RulesNotImplementedException(
            "no card has an ability, so none of them is waiting on a choice");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep) =>
        Chose(world, source, player, stoppedAt, input, tier);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep, eachPlayerFrame, finalPlayer);

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen) =>
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

            case Steps.PlaceThreatEffect:
                ApplyThreat(world, facts, abilities, events);
                break;

            case Steps.EnemiesActivate:
                PlanActivations(world, facts, step);
                break;

            case Steps.CompleteAttackActivation:
            case Steps.CompleteSchemeActivation:
                CompleteActivation(world, abilities, step, events);
                break;

            case Steps.ResumeAbility:
                events.AddRange(abilities.ResumeAbility(world, step));
                break;

            case Steps.FinalizeCharacterDefeat:
                Defeat.FinalizeCharacter(
                    world, facts, world.Cards[step.Subject], step.Trigger, events);
                break;

            case Steps.FinalizeSchemeDefeat:
                Defeat.FinalizeScheme(
                    world, facts, world.Cards[step.Subject], step.Trigger, events);
                break;

            case Steps.Scheme:
                Scheme(world, facts, abilities, world.Cards[step.Subject], step.Seat, events);
                break;

            case Steps.SchemeThreat:
                SchemeThreat(world, facts, abilities, step, events);
                break;

            case Steps.EndSchemeEarly:
                EndSchemeEarly(world);
                break;

            case Steps.Attack:
                Attack.Initiate(world, facts, step, events);
                break;

            case Steps.GiveBoostCard:
                Attack.GiveBoostCard(world, facts, events);
                break;

            case Steps.DeclareDefender:
                return Attack.DeclareDefender(world, facts, abilities);

            case Steps.FlipBoostCards:
                Attack.FlipBoostCards(world, facts, abilities, events);
                break;

            case Steps.CalculateAttackDamage:
                Attack.CalculateDamage(world, facts);
                break;

            case Steps.DealAttackDamage:
                Attack.DealDamage(world, facts, events);
                break;

            case Steps.NextAttackTarget:
                Attack.NextTarget(world, step.Seat);
                break;

            case Steps.CharacterAttacks:
                BasicPowers.ResolveCharacterAttack(world, facts, events, step.CharacterAttack);
                break;

            case Steps.CharacterThwarts:
                BasicPowers.ResolveCharacterThwart(world, facts, events, step.CharacterThwart);
                break;

            case Steps.AllyConsequentialDamage:
            case Steps.AllyThwartConsequentialDamage:
                AllyConsequentialDamage(world, facts, step, events);
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

            case Steps.DiscardRevealedTreachery:
                DiscardRevealedTreachery(world, facts, step, events);
                break;

            case Steps.ResolveSpecial:
                events.AddRange(abilities.ResolveSpecial(
                    world, world.Cards[step.Subject], step.Seat, step.FinalStep));
                break;

            case Steps.TurnAction:
                if (step.PlayerAction is not { } action)
                {
                    throw new InvalidOperationException(
                        "a player Action agenda step has no accepted action");
                }

                var occurrence = world.Agenda.Occurrence
                    ?? throw new InvalidOperationException(
                        "an applying player Action has no occurrence");
                try
                {
                    events.AddRange(abilities.Act(
                        world, action.Ability, action.Paying, action.Chosen, occurrence));
                }
                catch
                {
                    // A refused command must not become permanent agenda work.
                    // The engine chooses its command failure semantics; they
                    // match the former direct Action path, whose failed input
                    // left the open turn prompt retryable.
                    world.Agenda.Cancel(occurrence);
                    throw;
                }
                // Advance the owner while it is still first. Continuations
                // intentionally share its occurrence, so moving them ahead
                // first would make identity-based advancement select a child.
                world.Agenda.Advance(occurrence);
                world.Agenda.BeforeResponses(occurrence);
                break;

            case Steps.ChooseOption:
                return abilities.Choosing(
                    world, world.Cards[step.Subject], step.Seat, step.Index, step.Tier,
                    step.FinalStep, step.EachPlayerFrame, step.FinalPlayer);

            case Steps.ChooseAllyForLimit:
                return ChooseAllyForLimit(world, facts, step.Seat);

            case Steps.FinalizeAllyEntry:
                FinalizeAllyEntry(world, facts, abilities, step.Subject, events);
                break;

            case Steps.OrderEachPlayer:
                return EachPlayerEffects.Ordering(world, step);

            case Steps.ResolveEachPlayer:
                events.AddRange(EachPlayerEffects.Resolve(world, abilities, step));
                break;

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

            // Lifecycle steps exist to put their interrupt and response
            // windows on the agenda. The transition itself was applied before
            // the step was scheduled, so occurrence tier has nothing further
            // to mutate.
            case Steps.CardPlayed:
            case Steps.EventPlayed:
            case Steps.CardEntersPlay:
            case Steps.FormChanged:
                break;

            default:
                throw new RulesNotImplementedException(
                    $"the villain phase has no step '{step.What}'");
        }

        return null;
    }

    private static void CompleteActivation(
        World world, ICardAbilities abilities, PhaseStep step, List<GameEvent> events)
    {
        bool attacking = step.What == Steps.CompleteAttackActivation;
        var result = world.FinishedActivation is { } finished
            && finished.Id == step.ActivationId
            ? finished
            : new EnemyActivation(
                step.Subject, step.Seat, attacking, step.ActivationId, Made: false);

        world.FinishedActivation = result;
        events.AddRange(abilities.ActivationCompleted(world, result));
        world.FinishedActivation = null;
        world.Activation = null;
    }

    /// <summary>
    /// An ally's consequential damage — <c>rr:consequential-damage</c>.
    /// </summary>
    /// <remarks>
    /// Two facts, and they are not the same fact. <b>What the ally did</b> is
    /// the step's own name, and it is what the event stream records. <b>Which
    /// field it used</b> is <c>rr:assault.2</c>'s question — "it takes the
    /// consequential damage listed under its ATK instead of its THW" — and it
    /// is read here rather than when the step was scheduled, because assault
    /// is a constant ability and <c>rr:ability.9</c> makes those true only
    /// while their condition holds. A scheme that stopped being assaulted
    /// while the window was open stops sending the ally to its ATK icons.
    /// </remarks>
    private static void AllyConsequentialDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        bool attacked = string.Equals(
            step.What, Steps.AllyConsequentialDamage, StringComparison.Ordinal);

        BasicPowers.Consequential(
            world,
            facts,
            world.Cards[step.Subject],
            byAttack: attacked || BasicPowers.Assaulted(world, facts, world.Cards[step.Character]),
            attacked ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb,
            events);
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
                Attack.Defend(world, facts, abilities, input, events);
                break;

            case Steps.ChooseOption:
                events.AddRange(abilities.Chose(
                    world, world.Cards[step.Subject], step.Seat, step.Index, input, step.Tier,
                    step.FinalStep, step.EachPlayerFrame, step.FinalPlayer, step.Trigger));
                break;

            case Steps.ChooseAllyForLimit:
                DiscardAllyForLimit(world, facts, step.Seat, input, events);
                break;

            case Steps.OrderEachPlayer:
                EachPlayerEffects.Ordered(world, step, input);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    private static Prompt ChooseAllyForLimit(World world, ICardFacts facts, int player)
    {
        var allies = ControlledAllies(world, player);
        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (allies.Count <= limit)
        {
            throw new InvalidOperationException(
                $"player {player} no longer exceeds their ally limit");
        }

        return new Prompt(
            player,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAllyForLimit,
            $"{world.Seats[player].Name} chooses an ally to discard",
            false,
            [.. allies.Select(ally => new Affordance(
                ally.ObjectId,
                "Discard",
                ally.ObjectId,
                player,
                facts.Title(ally.FaceId)))]);
    }

    private static void DiscardAllyForLimit(
        World world, ICardFacts facts, int player, Decision input, List<GameEvent> events)
    {
        var ally = ControlledAllies(world, player)
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered for the ally limit");
        Discard.Card(world, ally, Steps.ChooseAllyForLimit, events);

        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (ControlledAllies(world, player).Count > limit)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.ChooseAllyForLimit,
                world.Agenda.Current?.Round ?? 0,
                0,
                Seat: player,
                Plan: true));
        }
    }

    private static List<Card> ControlledAllies(World world, int player) =>
    [
        .. world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .OrderBy(card => card.ObjectId),
    ];

    private static void FinalizeAllyEntry(
        World world, ICardFacts facts, ICardAbilities abilities, int allyId,
        List<GameEvent> events)
    {
        var ally = world.Cards[allyId];
        if (ally.Area.Type == DeckType.AlliesArea)
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: abilities);
        }
    }

    /// <summary>
    /// Step 2, one enemy at a time — <c>rr:villain-phase.step.2</c>, "in player
    /// order, each player resolves".
    /// </summary>
    private static void PlanActivations(World world, ICardFacts facts, PhaseStep step)
    {
        var playerOrder = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index >= playerOrder.Count)
        {
            return;
        }

        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return;
        }

        int seat = playerOrder[step.Index];
        var activated = step.ActivatedEnemies ?? [];

        // An eliminated seat remains in this procedure's stable order so its
        // removal cannot shift the next player under the current index. It has
        // no enemies left to activate; advance and clear the per-player set.
        if (world.Seats[seat].Eliminated)
        {
            world.Agenda.Then(step with
            {
                Index = step.Index + 1,
                ActivatedEnemies = [],
                ActivationPlayers = playerOrder,
                OccurrenceId = null,
            });
            return;
        }

        // `rr:activation.1`: hero form and the enemy attacks, alter-ego form
        // and it schemes. Read the form immediately before each activation:
        // an earlier activation can change it.
        var identity = world.Seats[seat].IdentityCard;
        bool attacking = facts.Kind(identity.FaceId) != CardKind.AlterEgo;

        int? enemy = !activated.Contains(villain.ObjectId)
            ? villain.ObjectId
            : world
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
                .Cards
                .OrderBy(minion => minion.ObjectId)
                .Select(minion => (int?)minion.ObjectId)
                .FirstOrDefault(candidate => candidate is { } id && !activated.Contains(id));

        if (enemy is { } next)
        {
            world.Agenda.Then(new PhaseStep(
                attacking ? Steps.Attack : Steps.Scheme,
                step.Round, 2, Index: seat, Subject: next, Seat: seat));
            world.Agenda.Then(step with
            {
                ActivatedEnemies = [.. activated, next],
                ActivationPlayers = playerOrder,
                OccurrenceId = null,
            });
            return;
        }

        // `rr:minion.4`: a minion that becomes engaged while engaged minions
        // are activating joins this procedure. The continuation above therefore
        // re-reads the area only after the preceding activation has completely
        // resolved. The list prevents a surviving minion from being chosen
        // again. Player-chosen ordering is not implemented; object-id order is
        // the engine's deterministic choice until that prompt exists.
        world.Agenda.Then(step with
        {
            Index = step.Index + 1,
            ActivatedEnemies = [],
            ActivationPlayers = playerOrder,
            OccurrenceId = null,
        });
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
        if (world.Agenda.Occurrence is { } occurrence)
        {
            Threat.Apply(world, facts, abilities, occurrence, events);
            occurrence.Also(Steps.VillainPhaseStepOneEnds);
        }
    }

    private static void ApplyThreat(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        var step = world.Agenda.Current
            ?? throw new InvalidOperationException("a threat step has no agenda item");
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a threat step has no occurrence");
        long placed = Threat.Apply(world, facts, abilities, occurrence, events);
        if (step.AbilityOccurrence is { } abilityOccurrence
            && step.Tier is { } tier
            && step.AbilityOrdinal >= 0
            && step.Placement is { Source: >= 0 } placement)
        {
            var ability = new PendingAbility(
                placement.Source, tier, step.Seat, step.AbilityOrdinal);
            if (placed > 0)
            {
                abilityOccurrence.Resolve(ability);
            }
            abilityOccurrence.Complete(ability);
        }
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

        // `rr:activation` -- the other kind, and the one that had no value on
        // the board until now. Set after `rr:stun-stunned`'s cancellation
        // above, because a cancelled activation is not one.
        world.Activation = new EnemyActivation(
            villain.ObjectId, seat, Attacking: false, Id: world.Agenda.Current?.ActivationId ?? -1);

        // **A scheming enemy holds boost cards, plural.**
        // `rr:scheme-enemy-activation.step.1` gives the card to the enemy --
        // "give **it** one facedown boost card" -- and step 2 resolves "each of
        // the scheming enemy's boost cards, one at a time and in the order in
        // which they were dealt", ending at `.step.2.e`: "if the enemy has any
        // boost cards remaining, repeat these steps with the next boost card."
        // That sentence cannot be true of a card drawn and discarded inside one
        // call, which is what this was: exactly one, with nowhere to put a
        // second. MARVEL-250.
        //
        // So the card goes where the rule puts it, on the enemy, and steps 1
        // and 2 become the two steps `rr:attack-enemy-activation` writes the
        // same way -- its step 1 word for word, and its step 3 sub-step for
        // sub-step, differing only in naming SCH where the attack names ATK.
        int round = world.Agenda.Current?.Round ?? 0;
        world.Agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, round, 1, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));
        world.Agenda.Then(new PhaseStep(
            Steps.FlipBoostCards, round, 2, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));

        // **Step 3 is a step, because step 2 can stop and ask.** A `Boost`
        // ability that offers the player a choice suspends, and the threat used
        // to go onto the scheme while the question was still on the table --
        // so whatever they chose arrived after the number it was meant to
        // change. The attack activation has the same shape:
        // `FlipBoostCards` is step 3 and `CalculateAttackDamage` is step 4.
        world.Agenda.Then(new PhaseStep(
            Steps.SchemeThreat,
            round,
            3,
            Index: seat,
            Subject: villain.ObjectId,
            Seat: seat,
            ActivationId: world.Activation.Id));
    }

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// "Place threat on the main scheme equal to the scheming enemy's
    /// <b>modified</b> SCH value." Modified is the word: the attack's own step
    /// reads a modified ATK, boost icons are registered as modifiers by step 2,
    /// and an attachment printing <c>SCH+</c> is one too.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step.</param>
    /// <param name="events">Where to record what happened.</param>
    private static void SchemeThreat(
        World world, ICardFacts facts, ICardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        long placed = 0;
        if (world.Agenda.Occurrence is { } occurrence)
        {
            placed = Threat.Apply(world, facts, abilities, occurrence, events);
            occurrence.Also(Steps.SchemeEnds);
        }

        // The other kind of activation ends here. A boost card's ability that
        // says "this activation" was given for *this* scheme and must not
        // survive into somebody's attack -- `rr:activation` makes a scheme an
        // activation, and `rr:activation.6` gives an activation an end.
        world.Effects.Expire(TimingPoints.EndOfActivation);
        if (world.Activation is { } activation)
        {
            world.FinishedActivation = activation with { ThreatPlaced = placed };
            world.Activation = null;
        }
    }

    /// <summary>Ends a scheme without placing threat when its minion left play.</summary>
    private static void EndSchemeEarly(World world)
    {
        world.Effects.Expire(TimingPoints.EndOfActivation);
        world.FinishedActivation = world.Activation;
        world.Activation = null;
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
        var revealOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a revealing card has no occurrence");
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
        World.MoveToTop(
            card,
            world.AreaOf(DeckType.RevealingArea, PlayArea.Of(player)));
        card.TurnFaceUp();
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = "villain phase", Verb = "Reveal",
        });

        // `rr:reveal.step.2` -- **where the card goes is decided by its type**,
        // and it happens before step 3's "When Revealed" abilities. A minion
        // that entered play is already engaged when its own ability resolves.
        Reveal.Resolve(world, facts, card, player, events, world.Agenda.Occurrence);

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
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a reveal has no occurrence");
        if (!abilities.CancelWhenRevealed(world, card, player, occurrence))
        {
            Reveal.Keywords(world, facts, abilities, card, player, events, occurrence);
            events.AddRange(abilities.WhenRevealed(world, card, player, occurrence));
        }

        // `rr:quickstrike.2` puts this after the card's own abilities, and it
        // is the one keyword that does something *after* them rather than
        // beside them.
        Reveal.Quickstrike(world, facts, card, player, round);

        // `rr:teamwork.2` puts this in the same place, after the card's own
        // abilities. A minion carrying both keywords activates twice, which is
        // what two forced responses to one moment do -- `rr:forced.5` gives the
        // first player their order, and asking is MARVEL-187.
        Reveal.Teamwork(world, facts, card, player, round);

        // Step 4. "If the card is a treachery, discard it." This is agenda
        // work rather than an inline move because `rr:treachery.2.1` keeps a
        // treachery whose last effect initiates activations faceup until all of
        // them finish. `Then` places this behind any activations or choices the
        // When Revealed text just scheduled.
        if (facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.DiscardRevealedTreachery,
                round,
                4,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true));

            // Reveal responses wait for all four reveal steps. Move both the
            // work initiated by the final effect and this discard continuation
            // ahead of that response window, preserving their scheduled order.
            world.Agenda.BeforeResponses(revealOccurrence);
        }
    }

    private static void DiscardRevealedTreachery(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        // The area check keeps an ability that moved the treachery from being
        // undone. The kind check makes a reconstructed agenda refuse stale or
        // malformed continuation data rather than discarding another type.
        if (facts.Kind(card.FaceId) != CardKind.Treachery
            || card.Area.Type != DeckType.RevealingArea)
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
