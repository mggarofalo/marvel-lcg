namespace Marvel.Rules.Prompts;

/// <summary>
/// What the game is asking a player for.
/// </summary>
/// <remarks>
/// <para>
/// One member per kind of question the Rules Reference describes. This is
/// <b>not</b> a timing: when a question is asked is
/// <see cref="Timing.TimingPriority"/>, which a prompt carries separately. The
/// rules never conflate the two, and neither does this — an interrupt and a
/// response are the same question asked in different tiers, not two questions.
/// </para>
/// <para>
/// The four members this replaces were a census of what one sample happened to
/// contain, which is a sample rather than a domain. These are read off the
/// rulebook instead.
/// </para>
/// </remarks>
public enum Question
{
    /// <summary>
    /// What to do on your turn — <c>rr:player-turn</c>, which lists six
    /// options: change form, play a card, use a basic power, use an ally,
    /// trigger an "Action" ability, or ask another player to trigger one.
    /// </summary>
    TurnOption,

    /// <summary>
    /// An opportunity to use an ability in an open window —
    /// <c>rr:first-player.4</c> and <c>.5</c>, "the first player has the first
    /// opportunity to use an interrupt / a response at each appropriate game
    /// moment". Which window it is, is the prompt's timing priority.
    /// </summary>
    Opportunity,

    /// <summary>
    /// Which game element an ability applies to — <c>rr:choose-game-element</c>.
    /// <c>rr:choose-game-element.1</c> settles who is asked: the player
    /// resolving the ability that uses the word "choose".
    /// </summary>
    Element,

    /// <summary>
    /// Which of an ability's listed options to take —
    /// <c>rr:choose-option</c>. Distinct from <see cref="Element"/> because the
    /// legality rules differ: <c>rr:choose-option.2</c> bars an option that
    /// cannot be at least partially resolved, including one whose cost the
    /// player cannot pay.
    /// </summary>
    Option,

    /// <summary>
    /// In what order two or more things resolve. The rules ask this in at least
    /// six places and give the answer to the same person each time:
    /// <c>rr:first-player.3</c> for simultaneous effects, <c>rr:forced.5</c> for
    /// simultaneous forced abilities, <c>rr:simultaneous-resolution</c> for a
    /// shared bold trigger, <c>rr:each-player.1</c> when an effect does not say,
    /// and <c>rr:activation.8.1</c> for activations initiated during another.
    /// The exceptions are the ones that belong to the player being acted on —
    /// <c>rr:villain-phase.step.2.b</c> and <c>rr:activation.5</c>, the order
    /// engaged enemies activate against you.
    /// </summary>
    Order,

    /// <summary>
    /// Which generators to spend — <c>rr:initiating-abilities.step.5</c>, and
    /// <c>rr:resource-ability.1</c> for the abilities triggerable while doing
    /// it.
    /// </summary>
    Payment,

    /// <summary>
    /// Which cards to discard — <c>rr:end-of-player-phase.step.1</c>, where a
    /// player <i>may</i> discard any number and <i>must</i> discard down to
    /// their hand size. Both halves are one question.
    /// </summary>
    Discard,

    /// <summary>
    /// Which character, if any, defends an enemy attack —
    /// <c>rr:attack-enemy-activation.step.2</c>, a step of its own with its own
    /// name. Not an <see cref="Element"/>: <c>rr:choose-game-element.1</c> puts
    /// that question to the player resolving an ability that says "choose", and
    /// nobody is resolving an ability here.
    /// </summary>
    Defender,
}

/// <summary>
/// One decision put to one player: what they may do, and why they are being
/// asked.
/// </summary>
/// <param name="Player">Whose decision this is.</param>
/// <param name="Asking">What is being asked for — see <see cref="Question"/>.</param>
/// <param name="When">
/// The tier this is being asked in — <see cref="Timing.TimingPriority"/>.
/// <c>Untimed</c> for a question that is not timed around an occurrence, which
/// a turn option is not.
/// </param>
/// <param name="Trigger">
/// The timing point that opened this, e.g. <c>WhenPlayerInTurn</c>. The same
/// string the event stream carries, so an event and the prompt it came from can
/// be tied together.
/// </param>
/// <param name="Label">
/// The domain-level prompt text, e.g. <c>"Spider-Man resolves mulligans"</c>.
/// </param>
/// <param name="Cancellable">
/// Whether declining is a legal answer. 81% of sampled prompts are cancellable,
/// which matters because 34.8% offer exactly one affordance — without this a
/// client cannot tell "your only move" from "your only move, or pass".
/// </param>
/// <param name="Affordances">What the player may do.</param>
/// <remarks>
/// <para>
/// This is the other half of the engine's return value:
/// </para>
/// <code>
/// (state, input) -> (state, Prompt?, GameEvent[])
/// </code>
/// <para>
/// A prompt is absent when the game is over. It is never empty: a decision with
/// no options is not put to a player. The event list, by contrast, is very often
/// empty — 35.3% of recorded steps change no state at all — so the two are
/// deliberately not symmetrical.
/// </para>
/// <para>
/// The numbers quoted throughout these types were measured once, over 30 games,
/// 1,997 prompts and 6,351 options. They are the sample that shaped the design;
/// nothing re-measures them.
/// </para>
/// </remarks>
public sealed record Prompt(
    int Player,
    Question Asking,
    Timing.TimingPriority When,
    string Trigger,
    string Label,
    bool Cancellable,
    IReadOnlyList<Affordance> Affordances);
