using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerStateProjection;

namespace Marvel.Cards.Run;

/// <summary>Projects villain stage settlement while retaining unknown stage-entry reads.</summary>
internal static class AbilityPowerVillainProjection
{
    internal static AbilityPowerState AdvancePowerVillain(
        AbilityPowerState state, Card damaged, Card first, AbilityAdmissionScope cast)
    {
        if (damaged.ObjectId != state.CurrentVillain
            && !AbilityPowerEncounterProjection.IsCurrent(damaged, cast, state)
            || PowerDamage(state, damaged) < PowerHealth(state, damaged, cast))
        {
            return state;
        }

        Card? next = NextStage(state, damaged, cast);
        bool carriesAttachments = next is not null && string.Equals(
            cast.World.Facts.Title(damaged.FaceId),
            cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        var discarded = DiscardedAfterStage(damaged, cast, state, carriesAttachments);
        string? stageEntry = StageEntryReadFailure(next, damaged, cast, state, discarded);
        var engagement = EngagementAfterStage(
            damaged, state, discarded, carriesAttachments);
        var settled = state with { Discarded = discarded, Engagement = engagement };
        return next is null
            ? CompleteVillain(settled, damaged, cast)
            : PromoteVillain(settled, damaged, next, first, cast, stageEntry);
    }

    private static AbilityPowerState CompleteVillain(
        AbilityPowerState state, Card damaged, AbilityAdmissionScope cast) =>
        state.EncounterStages is not null
            ? AbilityPowerEncounterProjection.Advance(state, damaged, null, cast)
            : state with { CurrentVillain = -1, Finished = true };

    private static AbilityPowerState PromoteVillain(
        AbilityPowerState state, Card damaged, Card next, Card first,
        AbilityAdmissionScope cast, string? stageEntry)
    {
        bool carriesTough = string.Equals(
                cast.World.Facts.Title(damaged.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)
            && PowerTough(state, damaged, cast);
        var advanced = state with
        {
            CurrentVillain = state.EncounterStages is null ? next.ObjectId : state.CurrentVillain,
            VillainStagesDrawn = state.EncounterStages is null
                ? state.VillainStagesDrawn + 1 : state.VillainStagesDrawn,
            UnprojectedStageEntry = stageEntry,
        };
        if (state.EncounterStages is not null)
            advanced = AbilityPowerEncounterProjection.Advance(advanced, damaged, next, cast);
        advanced = SetPowerDamage(advanced, next, 0, first, cast);
        bool printedTough = StateFields.Modified(
            cast.World, next, "toughness",
            cast.World.Facts, cast.World.Players) > 0;
        return SetPowerTough(
            advanced, next, carriesTough || printedTough, first, cast);
    }

    private static Card? NextStage(AbilityPowerState state, Card damaged, AbilityAdmissionScope cast)
    {
        if (state.EncounterStages is not null) return AbilityPowerEncounterProjection.Next(damaged, cast);
        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        return nextIndex >= 0 ? deck.Cards[nextIndex] : null;
    }

    private static HashSet<int> DiscardedAfterStage(
        Card damaged, AbilityAdmissionScope cast, AbilityPowerState state,
        bool carriesAttachments)
    {
        var discarded = new HashSet<int>(state.Discarded) { damaged.ObjectId };
        if (carriesAttachments) return discarded;
        foreach (int leaving in PowerLeavingTree(damaged, cast).Skip(1))
        {
            discarded.Add(leaving);
        }
        return discarded;
    }

    private static string? StageEntryReadFailure(
        Card? next, Card damaged, AbilityAdmissionScope cast,
        AbilityPowerState state, HashSet<int> discarded)
    {
        if (state.UnprojectedStageEntry is { } existing) return existing;
        if (next is null) return null;
        if (AbilityProgramQueries.On(cast.Context.Program, next).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            return $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads its constant abilities, "
                + "which is not implemented";
        }
        if (!HasLiveVillainRetargetingConstant(
            discarded, damaged, next, cast,
            threatChanges: state.SchemeThreat,
            damageChanges: state.CardDamage,
            modifierChanges: state.Modifiers,
            traitChanges: state.Traits,
            statusChanges:
            [
                .. state.StatusChanges,
                .. state.CardTough.Keys.Select(card => (card, Statuses.Tough)),
            ],
            engagementChanges: state.Engagement,
            formsMayChange: state.FormsMayChange,
            traceFirstPlayer: state.FirstPlayer))
        {
            return AbilityProgramQueries.On(cast.Context.Program, next).Any(ability =>
                ability.Trigger.Timing == AbilityType.WhenRevealed)
                ? $"villain stage '{next.FaceId}' has an unprojected When Revealed ability before a labelled-power continuation reads the board"
                : null;
        }
        return $"villain stage '{next.FaceId}' enters play before a "
            + "labelled-power continuation reads retargeting constant "
            + "abilities, which is not implemented";
    }

    private static Dictionary<int, int> EngagementAfterStage(
        Card damaged, AbilityPowerState state, HashSet<int> discarded,
        bool carriesAttachments)
    {
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (carriesAttachments) return engagement;
        foreach (int leaving in discarded.Where(cardId =>
            cardId != damaged.ObjectId && !state.Discarded.Contains(cardId)))
        {
            engagement.Remove(leaving);
        }
        return engagement;
    }

}
