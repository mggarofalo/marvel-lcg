using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>One resource ability's accepted costs and pending generation receipt.</summary>
internal sealed record ResourceAbilityContinuation(
    Card Source, int Player, CompiledCardAbility Ability, AbilityCostPayment Payment,
    ResourceGenerationReceipt Receipt) : CardAbilityContinuation;
