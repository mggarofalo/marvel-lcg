namespace Marvel.Rules.State;

/// <summary>The authority supplying a quantity before its modifiers.</summary>
public enum CardValueBaseKind
{
    /// <summary>The card's printed characteristics.</summary>
    Printed,
    /// <summary>An effect's replacement gameplay identity.</summary>
    Replacement,
    /// <summary>A default supplied by the rules, such as the ally limit.</summary>
    Rule,
    /// <summary>A signed adjustment whose consumer supplies the base.</summary>
    Adjustment,
}
