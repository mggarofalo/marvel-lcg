using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;

namespace Marvel.Cards.Run;

internal static class AbilityConditionProjection
{
    internal static AbilityCardSelection? TraceTestCard(AbilityCondition condition) =>
        condition switch
        {
            AbilityCondition.CardText
            {
                Property: AbilityCardTextProperty.Status
                    or AbilityCardTextProperty.Trait
                    or AbilityCardTextProperty.Title,
            } text => text.Card,
            AbilityCondition.IsKind kind => kind.Card,
            _ => null,
        };

    internal static bool TryTraceEnteredCardTest(
        AbilityCondition condition, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges, out bool result)
    {
        result = false;
        if (TraceTestCard(condition) is not { } selector
            || TraceEnteredCard(selector, discarded, cast) is not { } card) return false;
        result = condition switch
        {
            AbilityCondition.CardText
                { Property: AbilityCardTextProperty.Status } text =>
                statusChanges.Contains((card.ObjectId, text.Text)),
            AbilityCondition.CardText
                { Property: AbilityCardTextProperty.Trait } text =>
                Rules.State.Traits.Has(cast.World, card, text.Text, cast.World.Facts)
                || traitChanges.TryGetValue(card.ObjectId, out var traits)
                    && traits.Contains(text.Text),
            AbilityCondition.CardText
                { Property: AbilityCardTextProperty.Title } text =>
                string.Equals(cast.World.Facts.Title(card.FaceId), text.Text,
                    StringComparison.Ordinal),
            AbilityCondition.IsKind kind =>
                cast.World.Facts.Kind(card.FaceId) == kind.Kind,
            _ => throw new InvalidOperationException(
                "Unknown compiled test of an entered card"),
        };
        return true;
    }

    internal static bool ValueReadsVillain(
        AbilityCardSelection selector, AbilityAdmissionScope cast) =>
        PotentialVillainSelector(selector, cast) || selector switch
        {
            AbilityCardSelection.WithTrait filtered =>
                ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered =>
                ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.Discardable filtered =>
                ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.Ranked ranked => ValueReadsVillain(ranked.Cards, cast),
            _ => false,
        };

    internal static bool ValueReadsVillain(
        AbilityNumber number, AbilityAdmissionScope cast) => number switch
        {
            AbilityNumber.CardValue value => ValueReadsVillain(value.Card, cast),
            AbilityNumber.Counters counters => ValueReadsVillain(counters.Card, cast),
            AbilityNumber.Modified modified => ValueReadsVillain(modified.Card, cast),
            AbilityNumber.Count count => ValueReadsVillain(count.Cards, cast),
            AbilityNumber.Sum sum => sum.Operands.Any(value => ValueReadsVillain(value, cast)),
            AbilityNumber.Minimum minimum => minimum.Operands.Any(value => ValueReadsVillain(value, cast)),
            AbilityNumber.Product product => product.Operands.Any(value => ValueReadsVillain(value, cast)),
            AbilityNumber.Conditional conditional => ValueReadsVillain(conditional.Test, cast)
                || ValueReadsVillain(conditional.Then, cast)
                || ValueReadsVillain(conditional.Else, cast),
            _ => false,
        };

    internal static bool ValueReadsVillain(
        AbilityCondition condition, AbilityAdmissionScope cast) => condition switch
        {
            AbilityCondition.All all => all.Operands.Any(test => ValueReadsVillain(test, cast)),
            AbilityCondition.Any any => any.Operands.Any(test => ValueReadsVillain(test, cast)),
            AbilityCondition.Negated negated => ValueReadsVillain(negated.Operand, cast),
            AbilityCondition.AtLeast comparison =>
                ValueReadsVillain(comparison.Value, cast)
                || ValueReadsVillain(comparison.Count, cast),
            AbilityCondition.Exists exists => ValueReadsVillain(exists.Cards, cast),
            AbilityCondition.LegalPractice practice => ValueReadsVillain(practice.Schemes, cast),
            AbilityCondition.AutomaticThwart thwart => ValueReadsVillain(thwart.Scheme, cast),
            AbilityCondition.CardText text => ValueReadsVillain(text.Card, cast),
            AbilityCondition.IsKind kind => ValueReadsVillain(kind.Card, cast),
            AbilityCondition.WasDefeated defeated => ValueReadsVillain(defeated.Card, cast),
            AbilityCondition.IsYourIdentity identity => ValueReadsVillain(identity.Card, cast),
            _ => false,
        };

    internal static bool TryTraceConstantTest(
        AbilityCondition test, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, out bool result) =>
        VillainConditionTrace.Create(
            current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer).TryTest(test, out result);

    internal static bool AmountCanDifferInVillainTrace(
        AbilityNumber number, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth = 16) =>
        VillainConditionTrace.Create(
            current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer).AmountCanDiffer(number, dependencyDepth);

    internal static bool TestCanChangeOnVillainAdvance(
        AbilityCondition test, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth = 16) =>
        VillainConditionTrace.Create(
            current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer).TestCanChange(test, dependencyDepth);
}
