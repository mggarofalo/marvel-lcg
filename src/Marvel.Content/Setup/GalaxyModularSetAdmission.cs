using System.Collections.Immutable;

namespace Marvel.Content.Setup;

/// <summary>
/// Admits The Galaxy's Most Wanted's Galactic Artifacts, Kree Militants, Menagerie
/// Medley and Space Pirates modular sets, each as a product of its own.
/// </summary>
/// <remarks>
/// Ruling 408decdf6cd8c4b5 names these four among the box's eight modular encounter
/// sets. Admission is repository policy: every face authored and in <c>gmw</c>, and the
/// exact recipe, in its declared order, with the modular role. Physical quantities are
/// pinned to the vendored MarvelSDB snapshot. A missing face or changed recipe closes
/// that set alone.
/// </remarks>
public static class GalaxyModularSetAdmission
{
    private static readonly ImmutableArray<(string Key, ImmutableArray<string> Copies)> Sets =
    [
        ("galactic_artifacts", ["16122", "16123", "16124", "16125", "16126", "16127", "16128", "16129", "16130"]),
        ("kree_militant", ["16131", "16132", "16132", "16132", "16133", "16133", "16134", "16134", "16134"]),
        ("menagerie_medley", ["16135", "16135", "16135", "16135", "16136", "16136", "16136", "16137", "16137"]),
        ("space_pirates", ["16138", "16139", "16139", "16139", "16139", "16140", "16141", "16141"]),
    ];

    /// <summary>The four sets' setup keys, in declaration order.</summary>
    public static IEnumerable<string> Keys => Sets.Select(set => set.Key);

    /// <summary>The distinct faces of one of the four sets.</summary>
    public static ImmutableArray<string> Faces(string key) => [.. Copies(key).Distinct(StringComparer.Ordinal)];

    /// <summary>Whether one of the four sets is complete and declared as recorded.</summary>
    public static bool Admits(string key, SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(authored);
        return Faces(key).All(face => cards.Has(face) && cards.Pack(face) == "gmw" && authored.Contains(face))
            && setup.EncounterSetNames.Contains(key)
            && setup.EncounterRole(key) == EncounterSetRole.Modular
            && setup.EncounterSet(key).SequenceEqual(Copies(key));
    }

    private static ImmutableArray<string> Copies(string key) =>
        Sets.Single(set => set.Key == key).Copies;
}
