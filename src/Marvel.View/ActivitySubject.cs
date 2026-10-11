namespace Marvel.View;

/// <summary>An authorized occurrence name; it grants no historical face or art lookup.</summary>
public sealed record ActivitySubject(int Card, string Name, ActivitySubjectRole Role)
{
    /// <summary>The currently authorized reference has landscape scheme orientation.</summary>
    public bool Landscape { get; init; }
}
