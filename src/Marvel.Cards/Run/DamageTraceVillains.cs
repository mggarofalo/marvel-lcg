using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;

namespace Marvel.Cards.Run;

/// <summary>Settles villain stage replacement in a private repeated-damage trace.</summary>
internal sealed class DamageTraceVillains(DamageTraceState state, AbilityAdmissionScope cast)
{
    private int CurrentTough(int cardId) => state.Tough.GetValueOrDefault(
        cardId, Statuses.Count(cast.World, cast.World.Cards[cardId], Statuses.Tough));
    private void LeavePlay(int cardId) => new DamageTraceDepartures(state, cast).LeavePlay(cardId);

    internal DamageTraceState Resolve(Card card)
    {
        var next = Next(card);
        if (next is null)
        {
            LeavePlay(card.ObjectId);
            return state.EncounterStages is null ? state with { Finished = true } : AdvanceGroup(card, null);
        }
        bool carries = string.Equals(
            cast.World.Facts.Title(card.FaceId),
            cast.World.Facts.Title(next.FaceId), StringComparison.Ordinal);
        if (carries) state.Discarded.Add(card.ObjectId);
        else LeavePlay(card.ObjectId);
        RefuseUntraceableVillainEntry(card, next);
        return AdvanceVillain(card, next, carries);
    }

    private Card? Next(Card current)
    {
        if (state.EncounterStages is not null) return AbilityAreaEncounterProjection.Next(cast.World, current);
        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int index = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        return index < 0 ? null : deck.Cards[index];
    }

    private DamageTraceState AdvanceGroup(Card current, Card? next)
    {
        var topology = cast.World.EncounterTopology!;
        var group = topology.Groups.Single(group => group.Stages.Contains(current));
        var stages = new Dictionary<int, int>(state.EncounterStages!)
        { [group.Villain.ObjectId] = next?.ObjectId ?? -1 };
        var order = state.EncounterOrder!.Where(id => id != current.ObjectId);
        var advanced = state with
        {
            EncounterStages = stages,
            EncounterOrder = next is null ? order.ToArray() : [.. order, next.ObjectId],
        };
        if (next is not null)
            return state.CurrentVillain == current.ObjectId ? advanced with { CurrentVillain = next.ObjectId } : advanced;
        LeavePlay(group.Scheme.ObjectId);
        var living = topology.Groups.Where(group => stages[group.Villain.ObjectId] >= 0).ToArray();
        if (living.Length == 0) return advanced with { CurrentVillain = -1, Finished = true };
        return state.CurrentVillain == current.ObjectId ? SelectActive(advanced, living) : advanced;
    }

    private DamageTraceState SelectActive(DamageTraceState advanced, EncounterDeckGroup[] living)
    {
        long Threat(EncounterDeckGroup group) => state.Threat.GetValueOrDefault(group.Scheme.ObjectId,
            group.Scheme.Tokens.GetValueOrDefault("k_threat"));
        long maximum = living.Max(Threat);
        var candidates = living.Where(group => Threat(group) == maximum).ToArray();
        if (candidates.Length != 1)
            throw new RulesNotImplementedException("repeated damage requires the first player's tied-villain choice");
        return advanced with { CurrentVillain = advanced.EncounterStages![candidates[0].Villain.ObjectId] };
    }

    private void RefuseUntraceableVillainEntry(Card current, Card next)
    {
        if (AbilityProgramQueries.ActiveOn(cast.World, cast.Context.Program, next).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant
                && !AbilityInactiveAttackConstant.IsInactive(ability, cast.World)))
        {
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "repeated continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (HasLiveVillainRetargetingConstant(
            state.Discarded, current, next, cast, state.Threat, state.Damage, state.Modifiers, state.Traits,
            [
                .. state.StatusChanges,
                .. state.Tough.Keys.Select(card => (card, Statuses.Tough)),
            ], state.Engagement, state.FormsMayChange, state.FirstPlayer))
            throw new RulesNotImplementedException(
            $"villain stage '{next.FaceId}' enters play before a "
            + "repeated continuation reads retargeting constant abilities, "
            + "which is not implemented");
        if (AbilityProgramQueries.ActiveOn(cast.World, cast.Context.Program, next).Any(ability =>
                ability.Trigger.Timing == AbilityType.WhenRevealed))
        {
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' has an unprojected When Revealed ability before repeated continuation reads");
        }

    }

    private DamageTraceState AdvanceVillain(Card current, Card next, bool carries)
    {
        int carriedTough = carries ? CurrentTough(current.ObjectId) : 0;
        if (AbilityProjectedStatusInheritance.HasCarry(current, next, cast, state.StatusCounts))
            AbilityProjectedStatusInheritance.Apply(current, next, cast, state.StatusCounts, state.StatusChanges);
        state.Damage[next.ObjectId] = 0;
        state.Tough[next.ObjectId] = Math.Max(
            carriedTough,
            StateFields.Modified(
                cast.World, next, "toughness",
                cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
        return state.EncounterStages is null
            ? state with { CurrentVillain = next.ObjectId, VillainStagesDrawn = state.VillainStagesDrawn + 1 }
            : AdvanceGroup(current, next);
    }

}
