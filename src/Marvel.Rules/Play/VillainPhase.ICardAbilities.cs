using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

/// <summary>
/// What a card does when it is revealed from the encounter deck.
/// </summary>
/// <remarks>
/// This is the public compatibility composition surface. Rules services take
/// their exact consumer ports; hosts may supply one implementation that
/// satisfies all of them.
/// </remarks>
public interface ICardAbilities : IWindowAbilities, ICardCounterPools,
    IEncounterCardAbilities, ICardDamageAbilities, IThreatCardAbilities,
    ICardPowerAbilities, IResourceCardAbilities, ICardContinuationAbilities,
    IActivationCompletionAbilities, ICardReadinessAbilities,
    ICardSetupAbilities, ICardPlacementAbilities, ICardConstantAbilities, ICardActionAbilities,
    IAttackCardAbilities, ICardPlayAbilities, IRevealCardAbilities
{
}
