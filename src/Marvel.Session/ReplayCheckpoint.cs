using Marvel.Rules.Play;

namespace Marvel.Session;

/// <summary>
/// A verified game at the end of one save's active prefix, kept so the next
/// decision on that save is constructed without replaying from setup.
/// </summary>
/// <remarks>
/// <para>
/// A checkpoint is a cache. It holds the construction-mode game that derived
/// the save's newest decision, together with the history facts its replay
/// collected. Nothing outside replay and construction reads its game. It
/// answers only the exact save object it was made for; any other save, and
/// the loss of the checkpoint, means one full verifying replay instead.
/// </para>
/// <para>
/// A checkpoint is used once. The decision constructed on it changes its game,
/// so a failed candidate leaves no checkpoint behind.
/// </para>
/// </remarks>
public sealed class ReplayCheckpoint
{
    internal ReplayCheckpoint(SessionSave save, Game game, ActiveHistory history)
    {
        Save = save;
        Game = game;
        History = history;
    }

    internal SessionSave Save { get; }

    internal Game Game { get; }

    internal ActiveHistory History { get; }

    /// <summary>Whether this checkpoint was made for exactly this save.</summary>
    /// <remarks>
    /// Engine choice: identity, not equal content. A save-only publish such as an
    /// attach keeps the replayed units and only restamps the save, so a looser key
    /// would also be correct there; the identity check keeps the checkpoint's
    /// validity local instead of resting on how every publish builds its save.
    /// </remarks>
    internal bool Continues(SessionSave save) => ReferenceEquals(Save, save);
}
