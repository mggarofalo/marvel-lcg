using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Resolving a delayed effect that has come due.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:delayed-effect.1</c>: they "resolve automatically and immediately
/// after their specified timing point or future condition occurs or becomes
/// true, and before responses to that point or condition may be used". So this
/// is called at the occurrence and not from its response window, and
/// <c>rr:delayed-effect.2</c> is why nothing here goes into a window at all:
/// "it is not treated as a new triggered ability, even if the delayed effect
/// was originally created by a triggered ability".
/// </para>
/// <para>
/// <b>A kind is read, not called.</b> A delayed effect outlives the ability that
/// made it and has to survive a save, so <see cref="ContinuousEffect.Kind"/> is
/// a string this switches on rather than a closure the effect carries. See the
/// remarks on <see cref="ContinuousEffect"/>.
/// </para>
/// </remarks>
public static class DelayedEffects
{
    /// <summary>
    /// Discard the effect's own card from play — the shape "at the end of this
    /// attack, discard [this]".
    /// </summary>
    public const string DiscardFromPlay = "DiscardFromPlay";

    /// <summary>
    /// Stun whichever character the condition names — the shape "if a character
    /// is damaged by this attack, that character is stunned".
    /// </summary>
    /// <remarks>
    /// <b>The status is in the kind because there is nowhere else to put it.</b>
    /// <see cref="ContinuousEffect"/> carries a number, two card ids and a
    /// duration, and no string a status could live in — and it has to survive a
    /// save, so it cannot carry a closure either. Confusing or toughening the
    /// damaged character is another constant here, and a card that wants one
    /// adds it.
    /// </remarks>
    public const string StunTheSubject = "StunTheSubject";

    /// <summary>Resolve every delayed effect waiting on a condition that has occurred.</summary>
    /// <param name="world">The board.</param>
    /// <param name="condition">What has just happened.</param>
    /// <param name="events">Where to record what it did.</param>
    public static void Occur(World world, string condition, List<GameEvent> events) =>
        Occur(world, condition, subject: -1, events);

    /// <summary>
    /// Resolve every delayed effect waiting on a condition, against the card
    /// that condition happened to.
    /// </summary>
    /// <remarks>
    /// <b>An effect may be written before its target exists.</b> "If a character
    /// is damaged by this attack, that character is stunned" is created when the
    /// treachery is revealed and there is no damaged character yet — the attack
    /// has not happened. So the card is named by the occurrence when it comes
    /// due rather than by the effect when it is made, and
    /// <see cref="ContinuousEffect.Affects"/> is null for exactly that.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="condition">What has just happened.</param>
    /// <param name="subject">The card it happened to, or <c>-1</c>.</param>
    /// <param name="events">Where to record what it did.</param>
    public static void Occur(
        World world, string condition, int subject, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var effect in world.Effects.Occur(condition))
        {
            Resolve(world, effect, condition, subject, events);
        }
    }

    private static void Resolve(
        World world, ContinuousEffect effect, string condition, int subject,
        List<GameEvent> events)
    {
        switch (effect.Kind)
        {
            case DiscardFromPlay:
                Discard(world, effect, condition, events);
                break;

            case StunTheSubject:
                Stun(world, effect, condition, subject, events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"a delayed effect '{effect.Kind}' came due at '{condition}' and "
                    + "resolving that kind is not implemented");
        }
    }

    /// <summary>Stuns the card the condition named, if it named one.</summary>
    /// <remarks>
    /// Nothing happens when the condition named nobody, and that is the rule
    /// rather than a guard: "<b>if</b> a character is damaged by this attack"
    /// has a false branch, and an attack a tough status card ate damages
    /// nobody — <c>rr:tough.3</c>.
    /// </remarks>
    private static void Stun(
        World world, ContinuousEffect effect, string condition, int subject,
        List<GameEvent> events)
    {
        if (subject < 0 || subject >= world.Cards.Count)
        {
            return;
        }

        Reveal.Afflict(
            world, world.Facts, world.Cards[subject], Statuses.Stunned, condition, events);
    }

    private static void Discard(
        World world, ContinuousEffect effect, string condition, List<GameEvent> events)
    {
        if (effect.Affects is not int id || id < 0 || id >= world.Cards.Count)
        {
            throw new RulesNotImplementedException(
                $"a delayed effect due at '{condition}' would discard a card it does not name");
        }

        Play.Discard.Card(world, world.Cards[id], condition, events);
    }
}
