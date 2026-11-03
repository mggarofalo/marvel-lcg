namespace Marvel.Rules.State;

/// <summary>
/// A read open on a world: until it is disposed, nothing allocates an area,
/// a card or a game area.
/// </summary>
/// <remarks>
/// Open one around anything that reads the board and must leave it exactly as
/// it found it, such as the state digest or a client's projection. See
/// <see cref="WorldReads.BeginRead"/>.
/// </remarks>
public readonly struct WorldRead : IDisposable
{
    private readonly WorldAreas? areas;

    internal WorldRead(WorldAreas areas) => this.areas = areas;

    /// <summary>Closes the read.</summary>
    public void Dispose() => areas?.End();
}
