using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card-defined placement facts.</summary>
public interface ICardPlacementAbilities
{
    int? AttachesTo(World world, Card card);
    IReadOnlyList<int>? AttachmentTargets(World world, Card card);

    /// <summary>Whether active printed text permits this player to play a stored card.</summary>
    bool CanPlayFromStorage(World world, Card card, int player) => false;
}
#pragma warning restore CS1591
