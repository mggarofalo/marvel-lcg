using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Classifies authored encounter sets by their printed set icon.</summary>
public static class ModularEncounterSets
{
    /// <summary>Whether a named encounter set is selectable as a modular set.</summary>
    /// <remarks>
    /// The setup dataset supplies composition and the card catalog supplies the
    /// printed set icon. The engine chooses to use that joined fact as the one
    /// classification shared by discovery and deal validation.
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

        string icon = icons[0];
        return !icon.StartsWith("standard", StringComparison.Ordinal)
               && !icon.StartsWith("expert", StringComparison.Ordinal);
    }
}
