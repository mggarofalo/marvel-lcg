using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Places exactly the selected player-deck cards in a player's hand.</summary>
public sealed record SetPlayerHand(
    int Seat,
    IReadOnlyList<SceneCard> Cards)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-player-hand";
}
