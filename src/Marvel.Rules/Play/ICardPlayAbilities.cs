using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text used by card play and its enter-play transition.</summary>
public interface ICardPlayAbilities : ICardPlacementAbilities, IEncounterCardAbilities, ICardCounterPools
{
    /// <summary>Whether selected identities require first-ally history, including their inactive forms.</summary>
    bool RequiresAllyPlayHistory(World world);

    /// <summary>Read-only reduction supplied by this candidate's own printed play-cost text.</summary>
    long SelfPlayCostReduction(World world, Card card, int player);
}
#pragma warning restore CS1591
