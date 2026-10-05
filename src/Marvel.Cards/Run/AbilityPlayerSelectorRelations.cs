using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityPlayerSelectorRelations
{
    internal static bool RequiresChosenPlayer(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.InPlayerArea area => area.Player == AbilityPlayer.ChosenPlayer,
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.EnemiesEngagedWithChosenPlayer
           ,
        AbilityCardSelection.WithTrait filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.FaceDown filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Last filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.InObjectIdOrder filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Discardable filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Ranked ranked => RequiresChosenPlayer(ranked.Cards),
        _ => false,
    };

    internal static bool RebindsToEachPlayer(AbilityCardSelection targets) => targets switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is AbilityCardBinding.You
            or AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo,
        AbilityCardSelection.Query query => query.Kind == AbilityCardQuery.CharactersYouControl,
        AbilityCardSelection.WithTrait trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.FaceDown trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.Last trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.InObjectIdOrder trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.WithMatchingPlayerArea trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.Ranked ranked => RebindsToEachPlayer(ranked.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached other => RebindsToEachPlayer(other.Cards),
        _ => false,
    };

}
