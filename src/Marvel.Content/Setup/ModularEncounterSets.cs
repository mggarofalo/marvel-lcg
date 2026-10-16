using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Checks authored encounter-set roles and consistent printed membership.</summary>
public static class ModularEncounterSets
{
    /// <summary>Whether a named encounter set is selectable as a modular set.</summary>
    /// <remarks>
    /// The authored role is shared by discovery and deal admission. Printed set
    /// icons independently check that a named set has coherent membership.
    /// </remarks>
    public static bool IsModular(SetupCatalog catalog, ICardFacts facts, string name)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(name);

        var icons = catalog.EncounterSet(name)
            .SelectMany(spec => spec.Split(','))
            .Select(facts.EncounterSet)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (icons.Count != 1)
        {
            throw new ArgumentException(
                $"encounter set '{name}' contains {icons.Count} printed set icons",
                nameof(name));
        }

        return catalog.EncounterRole(name) == EncounterSetRole.Modular;
    }
}
