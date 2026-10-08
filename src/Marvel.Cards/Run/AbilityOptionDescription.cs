using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes direct public consequences without evaluating concealed branch information.</summary>
internal static class AbilityOptionDescription
{
    internal static (string? Label, string? Description) From(
        AbilityStructuralContext context, AbilityEffect option, AbilityEffect.Choose? alternatives = null)
    {
        if (option is AbilityEffect.Sequence { Effects.IsEmpty: true }
            && alternatives?.Options.Any(candidate => candidate is AbilityEffect.ChangeForm { Player: AbilityPlayer.You }) == true)
            return ("Remain in your current form", "Remain in your current form");
        if (AbilityPublicInstructionDescription.From(context, option) is { } instruction)
        {
            string label = char.ToUpperInvariant(instruction[0]) + instruction[1..];
            return (label, label);
        }
        return AbilityTargetedOptionDescription.From(context, option) ?? Direct(context, option);
    }

    private static (string? Label, string? Description) Direct(
        AbilityStructuralContext context, AbilityEffect option)
    {
        // These admitted relations name public objects. More general selectors
        // and numeric expressions can inspect hidden content in an unchosen
        // branch; they are not evaluated merely to explain an option.
        return option switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust,
                Selection: AbilityCardSelection.Query { Kind: AbilityCardQuery.CharactersYouControl } } =>
                ("Exhaust all your characters", null),
            AbilityEffect.EngageTopAsMinion
                { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You } } minions =>
                MinionOption(context, minions),
            AbilityEffect.Damage
                { Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You } } damage
                when AbilityPublicAmounts.IsFixed(damage.Amount) => DamageOption(context, damage),
            AbilityEffect.PlaceThreat
                { Schemes: AbilityCardSelection.Query { Kind: AbilityCardQuery.MainScheme }
                    or AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } } threat
                when AbilityPublicAmounts.IsFixed(threat.Amount) => ThreatOption(context, threat),
            _ => (AbilityEffectDescription.Summary(option), null),
        };
    }

    private static (string Label, string Description) MinionOption(
        AbilityStructuralContext context, AbilityEffect.EngageTopAsMinion minions)
    {
        // The compiled public profile names the minion, not the concealed card.
        string title = context.Program.Profiles[minions.Profile].Title;
        return ($"Engage {minions.Count} {title}",
            $"Put the top {minions.Count} card(s) of your deck into play facedown as {title}, engaged with you.");
    }

    private static (string Label, string Description) DamageOption(
        AbilityStructuralContext context, AbilityEffect.Damage damage)
    {
        World world = context.Expressions.World;
        Card recipient = AbilityStructuralFlowExecution.Every(damage.Cards, context).Single();
        long amount = AbilityStructuralQueries.Amount(damage.Amount, context.Expressions);
        long current = AbilityAmounts.SaturatingSum(amount,
            [AbilityEventModifiers.Amount(world, context.Expressions.Source, "eventDamage")]);
        string title = EffectiveCards.Title(recipient, world.Facts);
        string preview = Damage.PreviewDamage(world, world.Facts,
            context.Expressions.Source, recipient, current);
        return ($"Take {amount} damage: {title}",
            $"{preview} Interrupts and later effects can change the result.");
    }

    private static (string Label, string Description) ThreatOption(
        AbilityStructuralContext context, AbilityEffect.PlaceThreat threat)
    {
        World world = context.Expressions.World;
        Card scheme = AbilityStructuralFlowExecution.Every(threat.Schemes, context).Single();
        long amount = AbilityStructuralQueries.Amount(threat.Amount, context.Expressions);
        long current = scheme.Tokens.GetValueOrDefault("k_threat");
        long threshold = world.Facts.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        string state = threshold > 0 ? $"{current}/{threshold}" : current.ToString();
        return ($"Place {amount} threat on {world.Facts.Title(scheme.FaceId)}",
            $"{world.Facts.Title(scheme.FaceId)} currently has {state} threat. Interrupts and prevention can change the placement.");
    }
}
