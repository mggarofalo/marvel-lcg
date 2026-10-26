using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Emits one forced-ability receipt on initial initiation, including suspended resolutions.</summary>
internal static class AbilityWindowReceipt
{
    internal static void Emit(AbilityResolutionState cast, CompiledCardAbility ability, int ordinal)
    {
        if (ability.Trigger.Timing is not (AbilityType.ForcedInterrupt or AbilityType.ForcedResponse)) return;
        Associate(cast);
        cast.Events.Add(new AbilityWindowResult(cast.Source.ObjectId, ability.Trigger.Timing,
            cast.Player, cast.Occurrence.Id, Description(ability.Effect) ?? PrintedInstructions(cast), !cast.Suspended)
        {
            Ordinal = ordinal,
            Trigger = cast.Trigger,
            Verb = cast.Suspended ? "Ability_Initiated" : "Ability_Resolved",
            SubjectFaces = OccurrenceFaces.For(cast.Source),
            Subjects = new Dictionary<int, string>
                { [cast.Source.ObjectId] = EffectiveCards.Title(cast.Source, cast.World.Facts) },
        });
    }

    internal static void Associate(AbilityResolutionState cast)
    {
        if (cast.Execution is not { IsBatch: false } owner
            || owner.Ability.Type is not (AbilityType.ForcedInterrupt or AbilityType.ForcedResponse)) return;
        // This association is engine-chosen evidence, scoped to this actual ability execution.
        for (int index = 0; index < cast.Events.Count; index++)
            if (cast.Events[index].AbilitySource is null)
                cast.Events[index] = cast.Events[index] with
                { AbilitySource = owner.Ability.Card, AbilityOccurrence = owner.Occurrence.Id,
                    AbilityTiming = owner.Ability.Type, AbilityOrdinal = owner.Ability.Ordinal };
    }
    private static string PrintedInstructions(AbilityResolutionState cast)
    {
        // Printed text is vendored authority, retained whole when the typed instruction
        // vocabulary has no complete description. It is never parsed into decisions.
        string printed = cast.World.Facts.Text(EffectiveCards.FaceId(cast.Source));
        return string.IsNullOrWhiteSpace(printed) ? "Resolve the mandatory ability"
            : $"Printed instructions: {printed}";
    }

    private static string? Description(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => Sequence(sequence),
        AbilityEffect.GrantField { Cards: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.TriggerActor }, Until: "EndOfAttack" } grant =>
            $"The attacking enemy gains {grant.Field.ToLowerInvariant().Replace('_', ' ')} until the end of this attack",
        AbilityEffect.DelayedDiscard { Card: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.This }, Condition: "WhenAttackEnds" } =>
            "Discard this card at the end of this attack",
        _ => AbilityEffectDescription.Summary(effect),
    };

    private static string? Sequence(AbilityEffect.Sequence sequence)
    {
        string?[] descriptions = [.. sequence.Effects.Select(Description)];
        return descriptions.Any(string.IsNullOrWhiteSpace) ? null : string.Join("; ", descriptions);
    }
}
