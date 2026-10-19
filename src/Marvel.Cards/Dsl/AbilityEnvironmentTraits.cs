using System.Collections.Immutable;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilityEnvironmentTraits
{
    internal static void Validate(ImmutableArray<CompiledCardAbility> abilities,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect> effects)
    {
        foreach (var ability in abilities)
        {
            if (!IsCopy(ability.Effect)) continue;
            if (ability.Trigger.Timing != AbilityType.Constant || ability.Cost is not null
                || ability.When is not null || ability.Limit is not null || ability.Maximum is not null)
                throw new AbilityException("gainEnvironmentTraits requires an unconditional, cost-free constant");
        }
        foreach (var entry in effects)
        {
            if (IsCopy(entry.Value) && entry.Key.Path != "effect")
                throw new AbilityException("gainEnvironmentTraits must be at the constant ability root");
        }
    }

    private static bool IsCopy(AbilityEffect effect) => effect is AbilityEffect.Fixed
        { Instruction: AbilityFixedInstruction.GainEnvironmentTraits };
}
