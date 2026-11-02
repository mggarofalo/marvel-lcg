using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The in-play cards of the play areas a card selection may name.</summary>
internal static class AbilityPlayAreaCards
{
    /// <summary>
    /// The cards in <paramref name="area"/>'s in-play areas, read for <paramref name="player"/>,
    /// or null when <paramref name="area"/> names no play area.
    /// </summary>
    internal static Card[]? For(World world, AbilitySearchArea area, int player) => area switch
    {
        AbilitySearchArea.YourPlayArea => InPlay(world, playArea => playArea == PlayArea.Of(player)),
        // Equivalent mutant: no villain-area card is a Rocket Raccoon upgrade, so admitting
        // every play area survives.
        AbilitySearchArea.PlayerPlayAreas => InPlay(world, playArea => playArea.IsPlayers),
        AbilitySearchArea.VillainPlayArea => InPlay(world, playArea => playArea.IsVillains),
        _ => null,
    };

    private static Card[] InPlay(World world, Func<PlayArea, bool> playArea) =>
        [.. world.Areas.Where(candidate => DeckTypes.IsInPlay(candidate.Type)
            && playArea(candidate.PlayArea)).SelectMany(candidate => candidate.Cards)];
}
