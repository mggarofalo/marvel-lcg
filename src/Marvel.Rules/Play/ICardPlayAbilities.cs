using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text used by card play and its enter-play transition.</summary>
public interface ICardPlayAbilities : ICardPlacementAbilities, IEncounterCardAbilities, ICardCounterPools
{
}
#pragma warning restore CS1591
