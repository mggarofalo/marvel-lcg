using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A source-bound surcharge reserved by payment and completed after an event.</summary>
public static class NextEventCost
{
    // These effect and receipt names are engine choices, not printed vocabulary.
    private const string Increase = "nextEventCostIncrease";
    private const string Receipt = "nextEventCostReceipt";

    /// <summary>Whether this source can remain in its associated player's play area.</summary>
    public static bool CanRegister(World world, Card source, int player) =>
        player >= 0 && player < world.Players
        && source.Area.Type == DeckType.ObligationsArea
        && source.Area.PlayArea == PlayArea.Of(player)
        && !world.Effects.Active().Any(effect => effect.Kind == Increase
            && effect.Card == source.ObjectId && effect.TargetIncarnation == source.Incarnation);

    /// <summary>Keep a surcharge until this player's next event, with no phase expiration.</summary>
    public static void Register(World world, Card source, int player, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (!CanRegister(world, source, player))
            throw new RulesNotImplementedException("next-event surcharge requires an unmarked obligation in your play area");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Increase, amount, source.ObjectId, world.Seats[player].IdentityCard.ObjectId,
            new Duration(Uses: 1)) { TargetIncarnation = source.Incarnation });
    }

    /// <summary>Read applicable increases without spending them.</summary>
    public static IReadOnlyList<ContinuousEffect> Applied(World world, Seat seat, Card card) =>
        EffectiveCards.Kind(card, world.Facts) != CardKind.Event ? [] :
        world.Effects.Active().Where(effect => effect.Kind == Increase
            && effect.Affects == seat.IdentityCard.ObjectId && Current(world, effect)).ToArray();

    /// <summary>Bind the paid surcharge to this event before nested effects can play another.</summary>
    public static void Reserve(World world, Card card, AdjustedCardCost cost)
    {
        foreach (var effect in cost.Modifiers.Where(effect => effect.Kind == Increase))
            world.Effects.Register(effect with { Kind = Receipt, Affects = card.ObjectId });
    }

    /// <summary>Discard the original source after effects resolve or are canceled.</summary>
    public static void Complete(World world, Card card, List<GameEvent> events)
    {
        // rr:event.2: "the card is still considered to have been played" when effects are canceled.
        foreach (var effect in world.Effects.Active().Where(effect =>
            effect.Kind == Receipt && effect.Affects == card.ObjectId).ToArray())
        {
            world.Effects.Use(effect);
            if (Current(world, effect))
                Discard.Card(world, world.Cards[effect.Card!.Value], Steps.CardPlayed, events);
        }
    }

    private static bool Current(World world, ContinuousEffect effect) =>
        effect.Card is { } id && id >= 0 && id < world.Cards.Count
        && world.Cards[id] is { } source
        && source.Area.Type == DeckType.ObligationsArea
        && source.Incarnation == effect.TargetIncarnation;
}
