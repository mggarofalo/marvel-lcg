using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card-defined placement facts.</summary>
public interface ICardPlacementAbilities
{
    // Encounter placement supplies the resolving player; player-card queries
    // without a resolving player retain the card owner's context.
    int? AttachesTo(World world, Card card, int? player = null);
    IReadOnlyList<int>? AttachmentTargets(World world, Card card, int? player = null);

    /// <summary>Whether active printed text permits this player to play a stored card.</summary>
    bool CanPlayFromStorage(World world, Card card, int player);

    /// <summary>Whether the card grants its owner ordinary play from their discard pile.</summary>
    bool CanPlayFromDiscard(World world, Card card, int player);
}
#pragma warning restore CS1591
