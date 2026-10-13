namespace Marvel.View;

/// <summary>The canonical set of printed attributes carried to renderers.</summary>
internal static class PrintedFaceAttributes
{
    internal static Dictionary<string, string> PrintedStats(
        IReadOnlyDictionary<string, string> attributes)
    {
        string[] names =
        [
            "REC", "THW", "ATK", "DEF", "SCH", "HP", "HS", "Stage",
            "REC+", "THW+", "ATK+", "DEF+", "SCH+", "HP+",
            "StartingThreat", "TargetThreat", "EscalationThreat", "Boost", "RES", "Class",
            "Unique", "Acceleration", "Amplify", "Crisis", "Hazard",
        ];
        return names
            .Where(attributes.ContainsKey)
            .ToDictionary(name => name, name => attributes[name], StringComparer.Ordinal);
    }

}
