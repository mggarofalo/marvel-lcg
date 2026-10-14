namespace Marvel.View;

/// <summary>Visibility-safe semantic roles for grouping supporting receipts.</summary>
public enum EventActivityKind
{
    /// <summary>A consequence that retains its own reading beat.</summary>
    Consequence,
    /// <summary>Movement between non-hand locations.</summary>
    Travel,
    /// <summary>An authorized exposed card face.</summary>
    Reveal,
    /// <summary>Attachment to its explicit host.</summary>
    Installation,
    /// <summary>Completed boost value.</summary>
    Boost,
    /// <summary>Internal area graph bookkeeping, retained in diagnostics.</summary>
    Topology,
}
