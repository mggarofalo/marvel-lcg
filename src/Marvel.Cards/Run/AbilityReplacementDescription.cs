using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes a replacement instruction against its pending assignment.</summary>
internal static class AbilityReplacementDescription
{
    internal static string? Caption(AbilityResolutionExecution execution,
        World world, Card source, CompiledCardAbility ability, int player)
    {
        if (ability.Effect is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReplaceThreatWithDamage } replacement
            || world.Windows.Current?.Occurrence is not { Threat.Remaining: > 0 } occurrence)
            return null;
        var cast = new AbilityResolutionState(world, source, occurrence, player, [])
            { Tier = ability.Trigger.Timing };
        var context = execution.StructuralContext(cast);
        var recipient = new AbilitySelectorEvaluation(context.Expressions.Bindings).Find(replacement.Selection);
        if (recipient is null) return null;
        // The pending assignment supplies the amount; this describes the instruction,
        // not final damage after nested prevention and interrupt windows.
        long amount = occurrence.Threat.Remaining;
        return $"{EffectiveCards.Title(recipient, world.Facts)} takes {amount} damage instead of {amount} threat";
    }
}
