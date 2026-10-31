using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers the directly scoped, phase-bound printed-text instruction.</summary>
internal static class AbilityPrintedTextLowering
{
    // The admitted minion vocabulary has no alternate win/loss instruction.
    // rr:winning-the-game.1.1: "Alternate win or loss conditions cannot be
    // blanked or canceled." Such instructions require their own protection
    // contract; a minion kind alone is not evidence that all text is blankable.
    internal static void Validate(AbilityProgram program)
    {
        var byAbility = program.EffectsByAbility();
        foreach (var ability in program.Abilities)
        {
            var nodes = byAbility[(ability.Card, ability.Address.Ability)];
            var all = nodes.ToArray();
            ValidateTech(ability, all);
            if (!all.Any(node => node is AbilityEffect.GrantField { Field: PrintedText.Blanked })) continue;
            if (ability.Trigger.Timing != AbilityType.Action)
                throw new AbilityException("printed-text blanking is admitted only as a standalone action");
            if (ability.Effect is not AbilityEffect.GrantField
                { Field: PrintedText.Blanked, Cards: AbilityCardSelection.Bound
                    { Binding: AbilityCardBinding.This }, Until: TimingPoints.EndOfPhase,
                    EachCard: false, Amount: AbilityNumber.Constant { Value: 1 } })
                throw new AbilityException("printed-text blanking requires a standalone body; projected later effects are unsupported");
        }
    }

    private static void ValidateTech(CompiledCardAbility ability, AbilityEffect[] nodes)
    {
        if (!nodes.Any(node => node is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.BlankTechPlayerCards }))
            return;
        if (ability.Trigger.Timing != AbilityType.Constant || ability.Cost is not null
            || ability.When is not null || ability.Limit is not null || ability.Maximum is not null
            || ability.Effect is not AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.BlankTechPlayerCards })
            throw new AbilityException("blankTechPlayerCards requires a standalone unconditional cost-free constant");
    }

    internal static AbilityEffect Lower(AbilityValue value, AbilityLocation location)
    {
        AbilityLowering.FixedWord(value, location, "this");
        // The engine chooses a nonnumeric grant spelling so existing immutable
        // duration and target analyses share the same fixed target contract.
        return new AbilityEffect.GrantField(
            new AbilityCardSelection.Bound(AbilityCardBinding.This),
            PrintedText.Blanked, new AbilityNumber.Constant(1), false,
            TimingPoints.EndOfPhase);
    }
}
