using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Stacks selected cards on a player's deck; the first id is the next card drawn.</summary>
public sealed record StackPlayerDeck(
    int Seat,
    IReadOnlyList<SceneCard> TopFirst,
    PlayerDeckRemainder Remainder = PlayerDeckRemainder.Leave)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-player-deck";
}
