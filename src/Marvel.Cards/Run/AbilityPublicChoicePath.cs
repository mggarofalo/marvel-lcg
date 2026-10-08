using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Proves that reaching a checked choice does not select quantities through a private ancestor.</summary>
internal static class AbilityPublicChoicePath
{
    internal static bool AllowsAmount(AbilityStructuralContext context, AbilityEffect.ChooseCard choice) =>
        AllowsAmount(context.Program.On(context.AbilityFace)
            .Where(ability => context.Tier is null || ability.Trigger.Timing == context.Tier)
            .Select(ability => ability.Effect), choice);

    internal static bool AllowsAmount(IEnumerable<AbilityEffect> roots, AbilityEffect.ChooseCard choice)
    {
        // A persisted conditional frame records the branch, not its condition.
        // Inspect the exact checked node's authored ancestry without evaluating
        // game state. Missing or ambiguous provenance does not authorize a value.
        bool[] paths = roots.SelectMany(root => Paths(root, choice, publicPath: true)).ToArray();
        return paths is [true];
    }

    private static IEnumerable<bool> Paths(AbilityEffect node, AbilityEffect choice, bool publicPath)
    {
        if (ReferenceEquals(node, choice))
        {
            yield return publicPath;
            yield break;
        }
        bool next = publicPath && node switch
        {
            AbilityEffect.Sequence or AbilityEffect.Power => true,
            AbilityEffect.Conditional conditional => AbilityPublicAmounts.IsCurrentStep(conditional.Test),
            _ => false,
        };
        foreach (bool path in AbilityEffectStructure.AllEffectChildren(node)
            .SelectMany(child => Paths(child, choice, next)))
            yield return path;
    }
}
