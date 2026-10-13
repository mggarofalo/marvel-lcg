namespace Marvel.View;

/// <summary>An authorized occurrence identity, with an optional already-authorized printed reference.</summary>
public sealed record ActivitySubject(int Card, string Name, ActivitySubjectRole Role)
{
    /// <summary>The currently authorized reference has landscape scheme orientation.</summary>
    public bool Landscape { get; init; }

    /// <summary>Printed information authorized with this occurrence, without live or reconstructed historical state.</summary>
    public BoardCardPresentation? Reference { get; init; }
}
