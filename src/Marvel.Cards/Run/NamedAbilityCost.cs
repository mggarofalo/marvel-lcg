using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Binds a printed named-card cost to one controlled card in play.</summary>
internal static class NamedAbilityCost
{
    internal static Card RequireTarget(World world, int player, string title) =>
        Target(world, player, title)
        ?? throw new RulesNotImplementedException("Named card cost has no single controlled card");

    internal static Card? Target(World world, int player, string title)
    {
        if (player < 0) return null;
        // Engine choice: reject ambiguous named costs rather than selecting by object order.
        // rr:cost.7: "that player must pay costs with cards and/or game elements they control."
        Card[] matches = [.. world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
            && CardControl.ControllerOf(world, card) == player
            && world.Facts.Title(card.FaceId) == title).Take(2)];
        return matches.Length == 1 ? matches[0] : null;
    }
}
