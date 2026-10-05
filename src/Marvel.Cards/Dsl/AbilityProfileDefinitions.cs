using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

// Profile references are checked once when compiling the immutable book.
internal static class AbilityProfileDefinitions
{
    internal static ImmutableDictionary<string, EffectiveCardProfile> Compile(
        ImmutableArray<CompiledCardAbility> abilities,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect> effects)
    {
        var profiles = ImmutableDictionary.CreateBuilder<string, EffectiveCardProfile>(StringComparer.Ordinal);
        foreach (var ability in abilities)
        {
            if (ability.Effect is not AbilityEffect.DefineProfile definition) continue;
            var location = Location(ability.Address);
            if (ability.Trigger.Timing != AbilityType.Constant || ability.Cost is not null
                || ability.When is not null)
                throw location.Error("a profile definition must be an unconditional, cost-free constant");
            if (!profiles.TryAdd(definition.Profile.Id, definition.Profile))
                throw location.Error($"duplicate profile '{definition.Profile.Id}'");
        }
        foreach (var (address, effect) in effects.OrderBy(pair => pair.Key.Card, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.Ability).ThenBy(pair => pair.Key.Path, StringComparer.Ordinal))
        {
            if (effect is AbilityEffect.DefineProfile && address.Path != "effect")
                throw Location(address).Error("profile definitions must be at the constant ability root");
            if (effect is AbilityEffect.EngageTopAsMinion engagement && !profiles.ContainsKey(engagement.Profile))
                throw Location(address).Child("engageTopAsMinion/profile")
                    .Error($"unknown profile '{engagement.Profile}'");
        }
        return profiles.ToImmutable();
    }

    private static AbilityLocation Location(AbilityEffectAddress address) =>
        new(address.Card, address.Ability, address.Path);
}
