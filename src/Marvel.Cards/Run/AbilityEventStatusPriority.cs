using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Settles conflicting statuses before paid-event triggered interrupts.</summary>
internal static class AbilityEventStatusPriority
{
    internal static bool Cancel(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        var ability = execution.Pending(cast.Source, cast.ResolutionAbility!.Value);
        var labels = ability.Labels;
        // Engine choice: the bounded target-choice envelope preserves the one
        // printed power's label before that target is selected.
        var effect = ability.Effect;
        while (effect is AbilityEffect.ChooseCard choice) effect = choice.Effect;
        if (labels.Length == 0 && effect is AbilityEffect.Power power)
            labels = power.Kind switch
            {
                AbilityPowerKind.Attack => [BasicPowers.AttackVerb],
                AbilityPowerKind.Thwart => [BasicPowers.ThwartVerb],
                _ => [],
            };
        if (labels.Length == 0 || !LabeledAbilities.WouldBeCancelled(
                cast.World, cast.World.Facts, cast.Player, cast.Source, labels)) return false;
        // rr:status-cards.2: "Status card abilities have timing priority over
        // all conflicting triggered abilities." Costs have already been paid.
        LabeledAbilities.Begin(cast.World, cast.World.Facts, cast.Player,
            cast.Source, labels, cast.Events);
        return true;
    }
}
