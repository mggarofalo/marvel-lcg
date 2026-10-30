using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One ready instruction: the exact cards it readies together and the readies replaced.</summary>
/// <remarks>
/// <para>
/// <c>rr:end-of-player-phase.step.3</c>: "Each player simultaneously readies all
/// of their cards." A card effect readies its selected cards as one instruction
/// too. The cards are therefore fixed when the instruction begins, and a single
/// would-ready interrupt window serves all of them (<c>rr:triggering-condition.2</c>).
/// </para>
/// <para>
/// <c>rr:replacement-effect.1</c>: "When an effect is replaced, it is no longer
/// considered imminent and no further interrupts or responses to that effect can
/// be triggered." Replacement is recorded per card, so replacing one card's ready
/// leaves the others in the same instruction imminent.
/// </para>
/// </remarks>
public sealed class CardReadyBatch
{
    private readonly HashSet<int> replaced = [];

    /// <summary>Captures the cards an instruction readies, in its resolution order.</summary>
    /// <param name="cards">The cards eligible to ready when the instruction begins.</param>
    /// <param name="source">The card whose instruction readies them, or null when each card readies itself in a game step.</param>
    /// <param name="trigger">The event trigger text each ready records.</param>
    /// <param name="activity">The phase step these readies complete as one activity, if any.</param>
    public CardReadyBatch(IEnumerable<Card> cards, Card? source, string trigger, ActivityPhaseStep? activity = null)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(trigger);
        Targets = [.. cards.Select(card => card.ObjectId)];
        Source = source?.ObjectId;
        Trigger = trigger;
        Activity = activity;
    }

    /// <summary>The object ids of the cards this instruction readies, in resolution order.</summary>
    public ImmutableArray<int> Targets { get; }

    /// <summary>The instructing card, or null for a game step in which each card readies itself.</summary>
    public int? Source { get; }

    /// <summary>The trigger text recorded on each ready.</summary>
    public string Trigger { get; }

    /// <summary>The phase step these readies complete as one activity, if any.</summary>
    public ActivityPhaseStep? Activity { get; }

    /// <summary>Whether this instruction is still about to ready the card.</summary>
    /// <remarks>
    /// <c>rr:interrupt.3.1</c>: an interrupt that uses "would" resolves when its
    /// condition "becomes imminent". The card must be instructed, still exhausted,
    /// permitted to ready, and not already replaced. A card that leaves play is a
    /// new, ready copy (<c>rr:leaves-play.1</c>), so it no longer would ready.
    /// </remarks>
    public bool WouldReady(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return !replaced.Contains(card.ObjectId)
            && Targets.Contains(card.ObjectId)
            // The readiness port receives the instructing card, as each ready path
            // passed it; the authored book's prohibitions do not depend on it.
            && CardReadying.CanReady(world, card, Source is { } source ? world.Cards[source] : card);
    }

    /// <summary>Replaces this instruction's ready of one card, if it is still imminent.</summary>
    /// <returns>Whether the card's ready was imminent and is now replaced.</returns>
    public bool Replace(World world, Card card)
    {
        if (!WouldReady(world, card)) return false;
        replaced.Add(card.ObjectId);
        return true;
    }
}
