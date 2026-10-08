namespace Marvel.Rules.State;

/// <summary>Normalizes the engine's attribute notation without evaluating a printed value.</summary>
public static class PrintedStatFacts
{
    private static readonly string[] Names =
    [
        "Cost", "REC", "THW", "ATK", "DEF", "SCH", "HP", "HS", "Stage",
        "REC+", "THW+", "ATK+", "DEF+", "SCH+", "HP+",
        "StartingThreat", "TargetThreat", "EscalationThreat", "Boost",
        "Acceleration", "Amplify", "Crisis", "Hazard",
    ];

    /// <summary>Reads only printed numeric fields; an absent key remains absent.</summary>
    public static IReadOnlyDictionary<string, PrintedStatValue> From(
        CardKind kind, IReadOnlyDictionary<string, string> attributes) =>
        Names.Where(attributes.ContainsKey).ToDictionary(
            name => name,
            name => new PrintedStatValue(
                attributes[name].TrimEnd('*'), false,
                PrintedAttributeNotation.IsPerPlayer(kind, name, attributes[name]),
                PrintedAttributeNotation.ConsequentialDamage(kind, name, attributes[name])),
            StringComparer.Ordinal);
}
