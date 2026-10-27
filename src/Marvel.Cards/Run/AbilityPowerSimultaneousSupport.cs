using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a labelled power's independent effect and observation pair.</summary>
internal static class AbilityPowerSimultaneousSupport
{
    internal static AbilityEffect? Mutation(AbilityEffect node)
    {
        if (node is not AbilityEffect.Simultaneous { Effects.Length: 2 } simultaneous)
            return null;
        var first = simultaneous.Effects[0];
        var second = simultaneous.Effects[1];
        return Pair(first, second) ? first : Pair(second, first) ? second : null;
    }

    internal static bool ContainsPair(AbilityEffect node) => Mutation(node) is not null
        || AbilityProjection.StructuralChildren(node).Any(ContainsPair);

    internal static bool SupportsBody(AbilityEffect node)
    {
        if (Mutation(node) is not null) return true;
        if (node is not AbilityEffect.Sequence sequence || sequence.Effects.IsEmpty
            || Mutation(sequence.Effects[0]) is null) return false;
        var pair = (AbilityEffect.Simultaneous)sequence.Effects[0];
        var inspection = pair.Effects.OfType<AbilityEffect.InspectEncounterTop>().SingleOrDefault();
        return sequence.Effects.Skip(1).All(effect => Tail(effect, inspection?.Result));
    }

    internal static AbilityEffect? SuspensionMutation(AbilityEffect node) =>
        LiveReceiptMutation(node) ?? Mutation(node);

    private static AbilityEffect.AttackDamage? LiveReceiptMutation(AbilityEffect node) =>
        node is AbilityEffect.Sequence { Effects.IsEmpty: false } sequence
        && Mutation(sequence.Effects[0]) is AbilityEffect.AttackDamage damage
        && sequence.Effects.Skip(1).All(effect => Tail(effect, inspected: null))
            ? damage : null;

    private static bool Tail(AbilityEffect effect, string? inspected) => effect switch
    {
        AbilityEffect.AttackDamage
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Constant,
        } => inspected is null,
        AbilityEffect.GiveStatus
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
        } => inspected is null,
        AbilityEffect.RemoveThreat
        {
            Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Result result,
        } => inspected is not null && result.Name == inspected,
        AbilityEffect.Conditional conditional => inspected is null
            && ResourceTest(conditional.Test)
            && (conditional.Then is null || Tail(conditional.Then, inspected))
            && (conditional.Else is null || Tail(conditional.Else, inspected)),
        _ => false,
    };

    private static bool ResourceTest(AbilityCondition condition) => condition switch
    {
        AbilityCondition.DiscardedWithResource => true,
        AbilityCondition.All all => all.Operands.All(ResourceTest),
        AbilityCondition.Any any => any.Operands.All(ResourceTest),
        AbilityCondition.Negated negated => ResourceTest(negated.Operand),
        _ => false,
    };

    private static bool Pair(AbilityEffect mutation, AbilityEffect observation) =>
        mutation switch
        {
            AbilityEffect.AttackDamage
            {
                Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                Amount: AbilityNumber.Constant,
                SimultaneousPlacement: false,
            } => observation is AbilityEffect.DiscardTop
            {
                From: AbilitySearchArea.YourDeck, Players: null,
                Count: AbilityNumber.Constant { Value: 1 },
            },
            AbilityEffect.RemoveThreat
            {
                Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                Amount: AbilityNumber.Constant,
            } => observation is AbilityEffect.InspectEncounterTop { Count: 1 },
            _ => false,
        };
}
