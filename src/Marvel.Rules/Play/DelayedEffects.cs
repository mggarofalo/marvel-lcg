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

    /// <summary>Resolve every delayed effect waiting on a condition that has occurred.</summary>
    /// <param name="world">The board.</param>
    /// <param name="condition">What has just happened.</param>
    /// <param name="events">Where to record what it did.</param>
    public static void Occur(World world, string condition, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var effect in world.Effects.Occur(condition))
        {
            Resolve(world, effect, condition, events);
        }
    }

    private static void Resolve(
        World world, ContinuousEffect effect, string condition, List<GameEvent> events)
    {
        switch (effect.Kind)
        {
            case DiscardFromPlay:
                Discard(world, effect, condition, events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"a delayed effect '{effect.Kind}' came due at '{condition}' and "
                    + "resolving that kind is not implemented");
        }
    }

    private static void Discard(
        World world, ContinuousEffect effect, string condition, List<GameEvent> events)
    {
        if (effect.Affects is not int id || id < 0 || id >= world.Cards.Count)
        {
            throw new RulesNotImplementedException(
                $"a delayed effect due at '{condition}' would discard a card it does not name");
        }

        var card = world.Cards[id];

        // `rr:discard.1` -- an encounter card goes to the encounter discard
        // pile and a player card to its owner's. An attachment on the villain
        // is the scenario's, which is what owner -1 says.
        var discard = card.Owner < 0
            ? world.AreaOf(DeckType.EncounterDiscardPile)
            : world.AreaOf(DeckType.DiscardPile, PlayArea.Of(card.Owner), cardOwner: card.Owner);

        var from = card.Area;
        int host = from.Host;
        World.MoveToTop(card, discard);

        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = condition, Verb = "Discard",
        });

        if (host >= 0)
        {
            // `rr:attachment.4` -- an attachment that leaves play stops being
            // attached, and a client that drew it hanging off the villain has
            // to be told to take it away.
            events.Add(new CardDetached(card.ObjectId, host)
            {
                Trigger = condition, Verb = "Discard",
            });
        }
    }
}
