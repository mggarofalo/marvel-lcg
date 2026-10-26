using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Reads a live host's authored permission without changing the stored card's zone.</summary>
internal static class AbilityStoragePermission
{
    internal static bool CanPlay(World world, AbilityProgram program, Card card, int player)
    {
        if (Host(world, card, player) is not { } host) return false;
        // Effective profiles have no printed abilities; the storage permission cannot survive blanking.
        return AbilityProgramQueries.ActiveOn(world, program, host).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect is AbilityEffect.AllowPlayBeneath permission
            && EffectiveCards.Kind(card, world.Facts) == permission.Kind
            && Traits.Has(world, card, permission.Trait, world.Facts));
    }

    internal static bool CanPlayFromDiscard(World world, AbilityProgram program, Card card, int player)
    {
        if (player < 0 || player >= world.Seats.Count || !world.Cards.Contains(card)
            || card.Owner != player || (card.Area.Type != DeckType.DiscardPile || card.Area.PlayArea != PlayArea.Of(player))
            || EffectiveCards.Kind(card, world.Facts) != CardKind.Ally) return false;
        // Printed 05018: "from your discard pile during your turn (paying his resource cost)".
        // Turn timing is owned by the ordinary Play offer, not an Action ability.
        return AbilityProgramQueries.On(program, card).Any(ability =>
            ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false }
            && ability.Effect is AbilityEffect.AllowPlayFromDiscard);
    }

    private static Card? Host(World world, Card card, int player)
    {
        if (!world.Cards.Contains(card) || card.Owner != player || !BeneathCards.IsStored(card.Area.Type)) return null;
        int hostId = card.Area.Host;
        if (hostId < 0 || hostId >= world.Cards.Count) return null;
        var host = world.Cards[hostId];
        if (!DeckTypes.IsInPlay(host.Area.Type) || CardControl.ControllerOf(world, host) != player) return null;
        return host;
    }
}
