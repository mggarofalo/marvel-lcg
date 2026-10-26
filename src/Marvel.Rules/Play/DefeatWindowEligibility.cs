using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Recomputes the first eligible defeat interrupt tier after each completed ability.</summary>
internal static class DefeatWindowEligibility
{
    internal static IReadOnlyList<PendingAbility> Pending(World world, IWindowAbilities abilities, PhaseStep step)
    {
        if (step.Require<DefeatAbilityOperation>().Window.Sources.Count > 0)
            return [.. Instances(world, abilities, step).Select(instance => instance.Ability)];
        var operation = step.Require<DefeatAbilityOperation>();
        var occurrence = operation.Window.Origin
            ?? throw new InvalidOperationException("a defeat window has no occurrence");
        bool beforeDefeat = operation.Kind is DefeatAbilityKind.ChooseWouldBeDefeated
            or DefeatAbilityKind.ResumeWouldBeDefeated;
        var candidates = operation.Window.OwnAbilities.Where(ability => ability.TriggeringDefeat is not { } defeat
            || world.Cards[ability.Card].Incarnation == defeat.Incarnation
                && DeckTypes.IsInPlay(world.Cards[ability.Card].Area.Type)).Concat(
            abilities.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(ability => beforeDefeat || !operation.Window.OwnAbilities.Any(own =>
                    own.Card == ability.Card && own.Type == ability.Type && own.Ordinal == ability.Ordinal)));
        var tiers = AbilityWindow.Tiers(candidates, WindowKind.Interrupt, occurrence);
        if (tiers.Count == 0) return [];
        var (forced, optional) = AbilityWindow.Split(tiers[0]);
        return AbilityOfferHandles.Assign(forced.Count > 0 ? forced : optional, world.Cards.Count);
    }

    internal static Occurrence Origin(World world, IWindowAbilities abilities,
        PhaseStep step, PendingAbility ability) =>
        step.Require<DefeatAbilityOperation>().Window.Sources.Count == 0
            ? step.Require<DefeatAbilityOperation>().Window.Origin!
            : Instances(world, abilities, step).Single(instance => instance.Ability == ability).Origin;

    private static IReadOnlyList<(Occurrence Origin, PendingAbility Ability)> Instances(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var candidates = new List<(Occurrence Origin, PendingAbility Ability)>();
        foreach (var source in step.Require<DefeatAbilityOperation>().Window.Sources)
        {
            var card = world.Cards[source.Origin.Subject];
            // rr:would.1: changing the imminent condition invalidates its interrupts.
            // rr:leaves-play.1: re-entry is a new copy, outside this damage instruction.
            if (card.Incarnation != source.Incarnation || !DeckTypes.IsInPlay(card.Area.Type)
                || DamagePlacement.Health(world, world.Facts, card) - card.Damage > 0) continue;
            foreach (var tier in AbilityWindow.Tiers(
                abilities.Waiting(world, source.Origin, WindowKind.Interrupt), WindowKind.Interrupt, source.Origin))
                candidates.AddRange(tier.Abilities.Select(ability => (source.Origin, ability)));
        }
        if (candidates.Count == 0) return [];
        var first = candidates.Min(candidate => (AbilityTypes.PriorityOf(candidate.Ability.Type),
            candidate.Ability.Would ? 0 : 1));
        var tierInstances = candidates.Where(candidate =>
            (AbilityTypes.PriorityOf(candidate.Ability.Type), candidate.Ability.Would ? 0 : 1) == first).ToArray();
        var assigned = AbilityOfferHandles.Assign(tierInstances.Select(instance => instance.Ability), world.Cards.Count);
        return [.. tierInstances.Select((instance, index) => (instance.Origin, assigned[index]))];
    }
}
