namespace Marvel.Decisions;

/// <summary>Exposes engine-owned icon declarations for the current selected payment.</summary>
public static class DecisionResourceDeclarations
{
    /// <summary>Returns the declarations offered for one selected generator icon and cost component.</summary>
    public static IReadOnlyList<char> For(DecisionComposer composer, int source, int icon, int component)
    {
        ArgumentNullException.ThrowIfNull(composer);
        var cost = composer.SelectedCostOption();
        if (cost is null || !composer.Resources.Contains(source)
            || component < 0 || component >= cost.ResourceCosts.Count) return [];
        var matches = cost.Generators.Where(generator => generator.Effect == source).ToArray();
        if (matches.Length != 1 || icon < 0 || icon >= matches[0].Generates.Length) return [];
        return cost.ResourceCosts[component].Declarations(matches[0].Generates[icon]);
    }
}
