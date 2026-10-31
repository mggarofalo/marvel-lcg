using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Changes the open boost-icon count an interrupt was triggered by.</summary>
/// <remarks>
/// Scarlet Witch's Chaos Control counts the icons on a newly discarded card
/// instead; Scarlet Witch's Crest increases or decreases the number for this
/// count. Ruling 6962a015e8597900 defines the count; ruling 937d6ff4f970a1da
/// applies the Crest to a boost card turned faceup.
/// </remarks>
internal static class AbilityBoostCountEffects
{
    /// <summary>Whether the instruction changes the trigger's open count.</summary>
    /// <remarks>
    /// rr:initiating-abilities: an ability that would not change the game state
    /// cannot be initiated, so a decrease needs a positive count (rulings
    /// e90f1dff70bb0a76 and 4d45f47a56ec82a3 apply the same test to cancelling icons).
    /// A replacement needs only the open count; the discard before it is the change.
    /// </remarks>
    internal static bool CanApply(AbilityEffect node, Occurrence occurrence, World world) =>
        occurrence.BoostCount is { } count && count.IsCurrent(world)
        && (node is not AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.DecreaseBoostCount } || count.Value > 0);

    internal static bool TryRun(AbilityEffect effect, AbilityResolutionState cast)
    {
        if (effect is not AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.IncreaseBoostCount
            or AbilityFixedInstruction.DecreaseBoostCount or AbilityFixedInstruction.CountDiscardedInstead } fixedEffect)
            return false;
        var count = cast.Occurrence.BoostCount is { } open && open.IsCurrent(cast.World) ? open
            : throw new RulesNotImplementedException("a boost-icon count instruction requires its open count");
        bool applied = fixedEffect.Instruction switch
        {
            AbilityFixedInstruction.IncreaseBoostCount => Adjust(cast.World, count, 1),
            AbilityFixedInstruction.DecreaseBoostCount => count.Value > 0 && Adjust(cast.World, count, -1),
            _ => Replace(cast, count),
        };
        cast.CompletedOutcome = applied ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None;
        if (applied) cast.ResolveEffect();
        return true;
    }

    private static bool Adjust(World world, BoostIconCount count, long amount)
    {
        count.Adjust(world, amount);
        return true;
    }

    private static bool Replace(AbilityResolutionState cast, BoostIconCount count)
    {
        if (cast.Discarded.LastOrDefault() is not { } card) return false;
        count.Replace(cast.World, card);
        return true;
    }
}
