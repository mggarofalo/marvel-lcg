namespace Marvel.Acceptance.Tests.Play;

/// <summary>The faces of The Galaxy's Most Wanted modular sets admitted on their own.</summary>
internal static class GalaxyModularFaces
{
    private static readonly string[] KreeMilitants = ["16131", "16132", "16133", "16134"];
    private static readonly string[] MenagerieMedley = ["16135", "16136", "16137"];
    private static readonly string[] SpacePirates = ["16138", "16139", "16140", "16141"];

    internal static IReadOnlySet<string> Of(string set) => (set switch
    {
        "kree_militant" => KreeMilitants,
        "menagerie_medley" => MenagerieMedley,
        "space_pirates" => SpacePirates,
        _ => throw new ArgumentOutOfRangeException(nameof(set), set, "not an admitted Galaxy's Most Wanted modular set"),
    }).ToHashSet(StringComparer.Ordinal);
}
