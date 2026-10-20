using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Limits post-defeat projection to reads unaffected by facedown encounter dealing and reshuffling.</summary>
internal static class AbilityEncounterDealProjection
{
    internal static bool PreservesReads(AbilityEffect? effect) => effect switch
    {
        null => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(PreservesReads),
        AbilityEffect.Conditional conditional => Stable(conditional.Test)
            && PreservesReads(conditional.Then) && PreservesReads(conditional.Else),
        AbilityEffect.Draw => true,
        AbilityEffect.Damage damage => Stable(damage.Cards) && Stable(damage.Amount),
        AbilityEffect.AttackDamage damage => Stable(damage.Cards) && Stable(damage.Amount),
        AbilityEffect.GiveStatus status => Stable(status.Cards),
        AbilityEffect.Heal heal => Stable(heal.Card) && Stable(heal.Amount),
        _ => false,
    };

    private static bool Stable(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.All(Stable),
        AbilityCondition.Any any => any.Operands.All(Stable),
        AbilityCondition.Negated negated => Stable(negated.Operand),
        AbilityCondition.CardText text => Stable(text.Card),
        AbilityCondition.Exists exists => Stable(exists.Cards),
        AbilityCondition.AtLeast number => Stable(number.Value) && Stable(number.Count),
        AbilityCondition.IsKind kind => Stable(kind.Card),
        AbilityCondition.InForm or AbilityCondition.TitleInPlay => true,
        _ => false,
    };

    private static bool Stable(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result => true,
        AbilityNumber.Count count => Stable(count.Cards),
        AbilityNumber.Sum sum => sum.Operands.All(Stable),
        AbilityNumber.Product product => product.Operands.All(Stable),
        AbilityNumber.Minimum minimum => minimum.Operands.All(Stable),
        AbilityNumber.FloorDivide division => division.Operands.All(Stable),
        AbilityNumber.Conditional conditional => Stable(conditional.Test) && Stable(conditional.Then) && Stable(conditional.Else),
        _ => false,
    };

    private static bool Stable(AbilityCardSelection selection) => !AbilitySelectionAreaReads.For(selection)
        .Overlaps([DeckType.EncounterDeck, DeckType.EncounterDiscardPile, DeckType.DealtEncounterCardsDeck]);
}
