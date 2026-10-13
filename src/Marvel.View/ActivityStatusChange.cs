namespace Marvel.View;

/// <summary>The supplied status-area transition, independent of the resulting live badge.</summary>
public enum ActivityStatusChange
{
    /// <summary>This occurrence is not a status-area transition.</summary>
    None,
    /// <summary>A status entered its area.</summary>
    Applied,
    /// <summary>A status left its area.</summary>
    Removed,
}
