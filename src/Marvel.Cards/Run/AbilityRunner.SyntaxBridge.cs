using System.Globalization;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    // MARVEL-375 migration boundary: continuation and preflight callers still
    // supply syntax nodes. Join them to the already compiled instruction once
    // during construction; never lower content during gameplay. Remove this
    // lookup, along with compiledAbilities, when those callers use the program.
    private readonly Dictionary<AbilityNode, AbilityEffect> effectsBySyntax = [];

    private void IndexEffectSyntax(AbilityBook syntax)
    {
        var roots = new Dictionary<(string Card, int Ability), AbilityNode>();
        for (int index = 0; index < program.Abilities.Length; index++)
        {
            var address = program.Abilities[index].Address;
            roots.Add((address.Card, address.Ability), syntax.Abilities[index].Effect);
        }
        foreach (var (address, effect) in program.Effects
            .OrderBy(entry => entry.Key.Card, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key.Ability)
            .ThenBy(entry => entry.Key.Path, StringComparer.Ordinal))
        {
            AbilityValue value = NodeValue(roots[(address.Card, address.Ability)]);
            foreach (string segment in address.Path.Split('/').Skip(1))
            {
                value = value switch
                {
                    AbilityValue.Map map => map.Entries[segment],
                    AbilityValue.List list => list.Values[int.Parse(segment, CultureInfo.InvariantCulture)],
                    _ => throw new InvalidOperationException("Compiled effect address does not identify effect syntax"),
                };
            }
            // Re-reading a node creates a new wrapper around the same argument.
            // Record equality preserves that join. Equal scalar instructions
            // have the same meaning even when they occur at different addresses;
            // continuation identity remains the address, not this lookup key.
            effectsBySyntax[AbilityNode.Of(value)] = effect;
        }
    }

    private AbilityEffect CompiledEffect(AbilityNode node) =>
        effectsBySyntax.GetValueOrDefault(node)
        ?? throw new InvalidOperationException("Effect syntax is not part of the compiled ability program");

    private static AbilityEffect.Conditional ConditionalOf(AbilityNode node, Cast cast) =>
        (AbilityEffect.Conditional)((AbilityRunner)cast.Abilities).CompiledEffect(node);

    private static T EffectOf<T>(AbilityNode node, Cast cast) where T : AbilityEffect =>
        (T)((AbilityRunner)cast.Abilities).CompiledEffect(node);
}
