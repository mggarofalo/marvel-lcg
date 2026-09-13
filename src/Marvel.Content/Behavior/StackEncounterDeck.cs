using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Stacks selected encounter cards; the first id is the next card drawn.</summary>
public sealed record StackEncounterDeck(
    IReadOnlyList<SceneCard> TopFirst,
    EncounterDeckRemainder Remainder = EncounterDeckRemainder.Leave,
    int Seat = 0)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-encounter-deck";
}
