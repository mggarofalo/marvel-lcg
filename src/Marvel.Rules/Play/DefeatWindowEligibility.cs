using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Recomputes the first eligible defeat interrupt tier after each completed ability.</summary>
internal static class DefeatWindowEligibility
{
    internal static IReadOnlyList<PendingAbility> Pending(World world, IWindowAbilities abilities, PhaseStep step)
    {
        var operation = step.Require<DefeatAbilityOperation>();
        var occurrence = operation.Window.Origin
            ?? throw new InvalidOperationException("a defeat window has no occurrence");
        bool beforeDefeat = operation.Kind is DefeatAbilityKind.ChooseWouldBeDefeated
            or DefeatAbilityKind.ResumeWouldBeDefeated;
        var candidates = operation.Window.OwnAbilities.Concat(
            abilities.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(ability => beforeDefeat || ability.Card != step.Subject));
        var tiers = AbilityWindow.Tiers(candidates, WindowKind.Interrupt, occurrence);
        if (tiers.Count == 0) return [];
        var (forced, optional) = AbilityWindow.Split(tiers[0]);
        return AbilityOfferHandles.Assign(forced.Count > 0 ? forced : optional, world.Cards.Count);
    }
}
