namespace Marvel.View;

/// <summary>Readable names for authorized live-field keys; stable keys retain their identity.</summary>
public static class BoardFieldNames
{
    /// <summary>Describes a field without exposing its storage spelling.</summary>
    public static string Display(string name) => name.Replace("_", "", StringComparison.Ordinal)
        .ToUpperInvariant() switch
    {
        "BOOSTCONST" => "Boost",
        "QUICKSTRIKE" => "Quickstrike",
        "ATK" or "THW" or "DEF" or "REC" or "HP" => name.ToUpperInvariant(),
        _ => PromptPresentation.Words(name.Replace('_', ' ')),
    };
}
