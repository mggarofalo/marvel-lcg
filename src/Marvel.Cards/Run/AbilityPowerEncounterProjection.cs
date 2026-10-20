using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Projects stage membership without changing the live active counter or topology.</summary>
internal static class AbilityPowerEncounterProjection
{
    internal static Card? Current(Card card, AbilityAdmissionScope cast, IReadOnlyDictionary<int, int> stages)
    {
        var group = Group(card, cast);
        if (group is null) return card;
        int current = stages[group.Villain.ObjectId];
        return current < 0 ? null : cast.World.Cards[current];
    }

    internal static bool IsCurrent(Card card, AbilityAdmissionScope cast, AbilityPowerState state) =>
        state.EncounterStages is not null && Group(card, cast) is { } group
            && state.EncounterStages[group.Villain.ObjectId] == card.ObjectId;

    internal static Card? Next(Card card, AbilityAdmissionScope cast)
    {
        var group = Group(card, cast)
            ?? throw new RulesNotImplementedException("a projected villain has no immutable encounter group");
        int index = group.Stages.ToList().IndexOf(card);
        return index + 1 < group.Stages.Count ? group.Stages[index + 1] : null;
    }

    internal static AbilityPowerState Advance(
        AbilityPowerState state, Card damaged, Card? next, AbilityAdmissionScope cast)
    {
        var topology = cast.World.EncounterTopology!;
        var group = Group(damaged, cast)!;
        var stages = new Dictionary<int, int>(state.EncounterStages!)
        {
            [group.Villain.ObjectId] = next?.ObjectId ?? -1,
        };
        var updated = state with { EncounterStages = stages };
        if (next is not null)
            return damaged.ObjectId == state.CurrentVillain
                ? updated with { CurrentVillain = next.ObjectId } : updated;

        // pack:mc03:new-rules.multiple-villains-and-encounter-decks:
        // "When a villain is defeated, their side scheme is also removed".
        var departed = new HashSet<int>(state.Discarded);
        departed.UnionWith(AbilityPowerHealthTrace.PowerLeavingTree(group.Scheme, cast));
        updated = updated with { Discarded = departed };
        var living = topology.Groups.Where(candidate => stages[candidate.Villain.ObjectId] >= 0).ToArray();
        if (living.Length == 0) return updated with { CurrentVillain = -1, Finished = true };
        if (damaged.ObjectId != state.CurrentVillain) return updated;
        return SelectActive(updated, living);
    }

    private static AbilityPowerState SelectActive(AbilityPowerState state, EncounterDeckGroup[] living)
    {
        // pack:mc03:new-rules.the-active-villain: choose the villain whose
        // side scheme has the most threat; "the first player decides" ties.
        // Projection records that missing answer rather than selecting one.
        long Threat(EncounterDeckGroup group) =>
            state.SchemeThreat.GetValueOrDefault(group.Scheme.ObjectId,
                group.Scheme.Tokens.GetValueOrDefault("k_threat"));
        long maximum = living.Max(Threat);
        var candidates = living.Where(group => Threat(group) == maximum).ToArray();
        if (candidates.Length == 1)
            return state with { CurrentVillain = state.EncounterStages![candidates[0].Villain.ObjectId] };
        return state with
        {
            CurrentVillain = -1,
            UnprojectedStageEntry = state.UnprojectedStageEntry
                ?? "a projected active-villain defeat requires the first player's tied-villain choice",
        };
    }

    private static EncounterDeckGroup? Group(Card card, AbilityAdmissionScope cast) =>
        cast.World.EncounterTopology?.Groups.SingleOrDefault(group => group.Stages.Contains(card));
}
