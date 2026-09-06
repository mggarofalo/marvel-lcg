using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
{
    private static bool RequiresChosenPlayer(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.EnemiesEngagedWithChosenPlayer
            or AbilityCardQuery.TopmostTechInChosenDiscard,
        AbilityCardSelection.WithTrait filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Discardable filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Ranked ranked => RequiresChosenPlayer(ranked.Cards),
        _ => false,
    };

    private static bool RepeatedEffectCanChange(
        AbilityCondition test, AbilityEffect effect, AbilityAdmissionScope cast)
    {
        int original = cast.Player;
        try
        {
            var assumed = RepeatedChange.None;
            int priorFrames = Math.Max(0, cast.World.PlayerOrder.Count() - 1);
            for (int frame = 0; frame < priorFrames; frame++)
            {
                var observed = RepeatedChange.None;
                foreach (int player in cast.World.PlayerOrder)
                {
                    cast.RestorePlayer(player);
                    observed |= RepeatedChanges(
                        effect, cast, assumed, binding: false, priorFrames,
                        effect);
                }
                assumed |= observed;
            }
            return RepeatedTestCanChange(test, assumed);
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    private static RepeatedChange RepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding,
        int priorFrames, AbilityEffect repeatedEffect)
    {
        if (node.OperationName() == "changeForm")
        {
            return RepeatedChange.Form | RepeatedChange.CardsInPlay;
        }
        if (node.OperationName() is "dealDamage" or "indirectDamage" or "moveDamage"
            or "replaceThreatWithDamage")
        {
            return RepeatedChange.CardsInPlay
                | (DamageCanChangePlayerOrder(
                        node, cast, binding, repeatedEffect, assumed,
                        priorFrames)
                    ? RepeatedChange.PlayerOrder
                    : RepeatedChange.None);
        }
        if (node.OperationName() is "dealAttackDamage" or "moveAttackDamage")
        {
            return RepeatedChange.CardsInPlay;
        }
        if (node.OperationName() == "enemyAttacks")
        {
            return RepeatedChange.CardsInPlay | RepeatedChange.PlayerOrder;
        }
        if (StableForCardsInPlay(
            node, cast, priorFrames, repeatedEffect, assumed, binding))
        {
            return RepeatedChange.None;
        }
        if (node.OperationName() is "seq" or "then" or "otherwise")
        {
            var changes = RepeatedChange.None;
            foreach (var child in MutationChildren(node))
            {
                var next = RepeatedChanges(
                    child, cast, assumed | changes, binding, priorFrames,
                    repeatedEffect);
                changes |= next;
            }
            return changes;
        }
        if (node.OperationName() == "and")
        {
            var ordered = MutationChildren(node).ToList();
            var changes = RepeatedChange.None;
            for (int pass = 0; pass < ordered.Count; pass++)
            {
                var before = changes;
                foreach (var child in ordered)
                {
                    changes |= RepeatedChanges(
                        child, cast, assumed | changes, binding, priorFrames,
                        repeatedEffect);
                }
                if (changes == before)
                {
                    break;
                }
            }
            return changes;
        }
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            var branches = RepeatedTestCanChange(test, assumed)
                    || binding && BindingCanChange(test)
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Aggregate(
                RepeatedChange.None,
                (changes, branch) => changes
                    | RepeatedChanges(
                        branch, cast, assumed, binding, priorFrames,
                        repeatedEffect));
        }
        if (node.OperationName() is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            binding = true;
        }
        var children = MutationChildren(node).ToList();
        if (children.Count == 0)
        {
            return RepeatedChange.CardsInPlay;
        }
        return children.Aggregate(
            RepeatedChange.None,
            (changes, child) => changes
                | RepeatedChanges(
                    child, cast, assumed, binding, priorFrames,
                    repeatedEffect));
    }

    private static bool RepeatedTestCanChange(
        AbilityCondition test, RepeatedChange changes) => test switch
        {
            AbilityCondition.All all => all.Operands.Any(child =>
                RepeatedTestCanChange(child, changes)),
            AbilityCondition.Any any => any.Operands.Any(child =>
                RepeatedTestCanChange(child, changes)),
            AbilityCondition.Negated negated => RepeatedTestCanChange(negated.Operand, changes),
            AbilityCondition.InForm form => changes.HasFlag(RepeatedChange.Form)
                || changes.HasFlag(RepeatedChange.PlayerOrder)
                    && form.Player != AbilityPlayer.You,
            AbilityCondition.TitleInPlay => changes.HasFlag(RepeatedChange.CardsInPlay),
            AbilityCondition.Flag { Kind: AbilityConditionFact.FinalStep }
                or AbilityCondition.PaidWithResource or AbilityCondition.CausedThreat => false,
            _ => true,
        };

    [Flags]
    private enum RepeatedChange
    {
        None = 0,
        Form = 1,
        CardsInPlay = 2,
        PlayerOrder = 4,
    }

    private static bool StableForCardsInPlay(
        AbilityEffect node, AbilityAdmissionScope cast, int priorFrames,
        AbilityEffect repeatedEffect, RepeatedChange assumed,
        bool binding) =>
        node.OperationName() is "draw" or "drawToHandSize" or "drawToPrintedHandSize"
            or "exhaust" or "ready" or "heal" or "generate" or "giveStatus"
            or "gainSurge" or "preventDamage" or "preventThreat"
            or "cancelWhenRevealed" or "cancelOccurrence" or "grantUntil"
            or "grantCharactersControlledBy" or "reduceNextCardCost"
        || node.OperationName() == "removeThreat"
            && Every(ThreatSelectionOf(node, cast), cast) is { Count: > 0 } schemes
            && schemes.All(scheme => scheme.Area.Type == DeckType.MainSchemesArea
                || !CanExhaust(
                    // An earlier ordered mutation can switch a branch before
                    // this leaf is reached in the same repeated frame.
                    TotalThreatRemoved(
                        scheme, repeatedEffect, cast, assumed, binding),
                    priorFrames,
                    scheme.Tokens.GetValueOrDefault("k_threat")));

    private static bool DamageCanChangePlayerOrder(
        AbilityEffect node, AbilityAdmissionScope cast, bool binding,
        AbilityEffect repeatedEffect, RepeatedChange assumed,
        int priorFrames)
    {
        AbilityCardSelection targets = node.OperationName() switch
        {
            "dealDamage" or "indirectDamage" => DamageSelectionOf(node, cast),
            "moveDamage" => EffectOf<AbilityEffect.MoveDamage>(node, cast).To,
            "replaceThreatWithDamage" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection,
            _ => throw new InvalidOperationException(
                $"'{node.OperationName()}' is not a direct damage node"),
        };
        int damagingFrames = RebindsToEachPlayer(targets)
            ? 1
            : priorFrames;
        var cards = Every(targets, cast);
        return cards.Any(card => cast.World.PlayerOrder.Any(player =>
                cast.World.Seats[player].IdentityCard == card)
            && TotalRepeatedDamageTo(
                card, repeatedEffect, cast, assumed, binding,
                damagingFrames)
                >= Damage.Health(cast.World, cast.World.Facts, card) - card.Damage)
            || cards.Count == 0 && binding && BindingCanChange(targets);
    }

    private static long TotalRepeatedDamageTo(
        Card target, AbilityEffect repeatedEffect, AbilityAdmissionScope cast,
        RepeatedChange assumed, bool binding, int frames)
        => PeakRepeatedDamageOn(
            target, repeatedEffect, cast, assumed, binding, frames)
            - target.Damage;

    private static bool RebindsToEachPlayer(AbilityCardSelection targets) => targets switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is AbilityCardBinding.You
            or AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo,
        AbilityCardSelection.Query query => query.Kind == AbilityCardQuery.CharactersYouControl,
        AbilityCardSelection.WithTrait trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.Ranked ranked => RebindsToEachPlayer(ranked.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached other => RebindsToEachPlayer(other.Cards),
        _ => false,
    };

    private static long TotalThreatRemoved(
        Card scheme, AbilityEffect node, AbilityAdmissionScope cast,
        RepeatedChange assumed = RepeatedChange.None, bool binding = false)
    {
        long own = node.OperationName() == "removeThreat"
            && Every(ThreatSelectionOf(node, cast), cast).Any(candidate =>
                candidate.ObjectId == scheme.ObjectId)
                ? Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast)
                : 0;
        return MutationTotal(
            node, cast, assumed, binding, own,
            child => TotalThreatRemoved(
                scheme, child, cast, assumed, binding));
    }

    private readonly record struct DamageTransfer(
        int From, int To, long Amount,
        bool GrantsHealth = false, bool DealsDamage = false,
        bool Discards = false, bool EntersPlay = false,
        bool RemovesThreat = false,
        bool PlacesThreat = false, string? GrantsTrait = null,
        string? GrantsField = null,
        AbilityCardSelection? FromVillain = null, AbilityCardSelection? ToVillain = null,
        int ChangesForm = -1, string? Form = null,
        string? GrantsStatus = null);

    private readonly record struct TraceCard(
        Card Card, AbilityCardSelection? VillainSelector);

    private sealed record DamageTraceState(
        Dictionary<int, long> Damage, long PeakTargetPressure,
        HashSet<int> Players, Dictionary<int, int> Tough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, long> Health, HashSet<int> Discarded,
        Dictionary<int, long> Threat, Dictionary<int, HashSet<string>> Traits,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, int> Engagement,
        ulong FormsMayChange,
        int FirstPlayer,
        int CurrentVillain,
        int VillainStagesDrawn, bool Finished);

    private static long PeakRepeatedDamageOn(
        Card target, AbilityEffect repeatedEffect, AbilityAdmissionScope cast,
        RepeatedChange assumed, bool binding, int frames)
    {
        int original = cast.Player;
        try
        {
            int villain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
            IReadOnlyList<DamageTraceState> states =
                [new(new Dictionary<int, long>(), target.Damage, [], new(), new(),
                    new(), new(), TraceUnavailableMinions(cast), new(),
                    new(), new(), new(), 0, cast.World.FirstPlayer,
                    villain, 0, false)];
            for (int frame = 0; frame < frames; frame++)
            {
                var next = new List<DamageTraceState>();
                foreach (var state in states)
                {
                    foreach (int player in cast.World.PlayerOrder.Where(player =>
                        !state.Players.Contains(player)))
                    {
                        cast.RestorePlayer(player);
                        var traces = DamageTraces(
                            repeatedEffect, cast, assumed, binding);
                        foreach (var trace in traces)
                        {
                            var advanced = ApplyDamageTrace(
                                state, trace, target, cast);
                            next.Add(advanced with
                            {
                                Players = [.. state.Players, player],
                            });
                        }
                    }
                }
                states = next;
            }
            return states.Select(state => state.PeakTargetPressure)
                .DefaultIfEmpty(target.Damage)
                .Max();
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    private static HashSet<int> TraceUnavailableMinions(AbilityAdmissionScope cast) =>
        cast.World.Areas
            .SelectMany(area => area.Cards)
            .Where(card => card.Area.Type != DeckType.EngagedEnemiesArea
                && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion)
            .Select(card => card.ObjectId)
            .ToHashSet();

    private static DamageTraceState ApplyDamageTrace(
        DamageTraceState state, IReadOnlyList<DamageTransfer> trace,
        Card target, AbilityAdmissionScope cast)
    {
        var damage = new Dictionary<int, long>(state.Damage);
        var tough = new Dictionary<int, int>(state.Tough);
        var statusChanges = new HashSet<(int Card, string Status)>(
            state.StatusChanges);
        var statusCounts = new Dictionary<(int Card, string Status), int>(
            state.StatusCounts);
        var health = new Dictionary<int, long>(state.Health);
        var discarded = new HashSet<int>(state.Discarded);
        var threat = new Dictionary<int, long>(state.Threat);
        var traits = state.Traits.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(state.Modifiers);
        var engagement = new Dictionary<int, int>(state.Engagement);
        ulong formsMayChange = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        int currentVillain = state.CurrentVillain;
        int villainStagesDrawn = state.VillainStagesDrawn;
        bool finished = state.Finished;
        long peak = state.PeakTargetPressure;
        long Current(int card) => damage.TryGetValue(card, out long amount)
            ? amount
            : cast.World.Cards[card].Damage;
        int CurrentTough(int card) => tough.TryGetValue(card, out int count)
            ? count
            : Statuses.Count(cast.World, cast.World.Cards[card], Statuses.Tough);
        long HealthBonus(int card) => health.GetValueOrDefault(card);
        void ObserveTarget() => peak = Math.Max(
            peak, Current(target.ObjectId) - HealthBonus(target.ObjectId));
        long CurrentThreat(int card) => threat.TryGetValue(card, out long amount)
            ? amount
            : cast.World.Cards[card].Tokens.GetValueOrDefault("k_threat");
        void LeavePlay(int cardId)
        {
            var leaving = new List<int> { cardId };
            var pending = new Stack<Card>(cast.World.Areas
                .Where(area => area.Host == cardId)
                .SelectMany(area => area.Cards)
                .Reverse());
            var seen = new HashSet<int> { cardId };
            while (pending.TryPop(out var hosted))
            {
                if (!seen.Add(hosted.ObjectId))
                {
                    throw new RulesNotImplementedException(
                        $"attachment {hosted.ObjectId} forms a hosting cycle");
                }
                if (StateFields.Modified(
                        cast.World, hosted, "permanent",
                        cast.World.Facts, cast.World.Players) > 0)
                {
                    // Match Discard.Attachments' complete-tree preflight so
                    // eligibility refuses before an action cost can mutate.
                    throw new RulesNotImplementedException(
                        $"permanent attachment {hosted.ObjectId} lost host "
                        + $"{cardId}, and rr:permanent.5 is not implemented");
                }

                leaving.Add(hosted.ObjectId);
                foreach (var child in cast.World.Areas
                    .Where(area => area.Host == hosted.ObjectId)
                    .SelectMany(area => area.Cards)
                    .Reverse())
                {
                    pending.Push(child);
                }
            }

            foreach (int leavingId in leaving)
            {
                discarded.Add(leavingId);
                if (Statuses.Count(
                    cast.World, cast.World.Cards[leavingId], Statuses.Tough) > 0)
                {
                    tough[leavingId] = 0;
                }
                else
                {
                    tough.Remove(leavingId);
                }
                TraceStatusesLeave(
                    leavingId, cast, statusCounts, statusChanges);
                engagement.Remove(leavingId);
            }
        }
        void ResolveCharacterDefeat(int cardId)
        {
            var card = cast.World.Cards[cardId];
            long tracedHealth = SaturatingAdd(
                TraceHealth(card, discarded, threat, cast),
                HealthBonus(cardId));
            if (cardId == currentVillain
                && Current(cardId) >= tracedHealth)
            {
                var deck = cast.World.AreaOf(DeckType.VillainDeck);
                int nextIndex = deck.Cards.Count - 1 - villainStagesDrawn;
                if (nextIndex < 0)
                {
                    LeavePlay(cardId);
                    finished = true;
                    return;
                }

                var next = deck.Cards[nextIndex];
                bool carriesAttachments = string.Equals(
                    cast.World.Facts.Title(card.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal);
                if (carriesAttachments)
                {
                    discarded.Add(cardId);
                }
                else
                {
                    LeavePlay(cardId);
                }
                if (cast.Context.Program.On(next.FaceId).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant))
                {
                    throw new RulesNotImplementedException(
                        $"villain stage '{next.FaceId}' enters play before a "
                        + "repeated continuation reads its constant abilities, "
                        + "which is not implemented");
                }
                if (HasLiveVillainRetargetingConstant(
                    discarded, card, next, cast,
                    threatChanges: threat,
                    damageChanges: damage,
                    modifierChanges: modifiers,
                    traitChanges: traits,
                    statusChanges:
                    [
                        .. statusChanges,
                        .. tough.Keys.Select(card => (card, Statuses.Tough)),
                    ],
                    engagementChanges: engagement,
                    formsMayChange: formsMayChange,
                    traceFirstPlayer: firstPlayer))
                {
                    throw new RulesNotImplementedException(
                        $"villain stage '{next.FaceId}' enters play before a "
                        + "repeated continuation reads retargeting constant "
                        + "abilities, which is not implemented");
                }
                villainStagesDrawn++;
                int carriedTough = carriesAttachments
                    ? CurrentTough(cardId)
                    : 0;
                currentVillain = next.ObjectId;
                damage[next.ObjectId] = 0;
                tough[next.ObjectId] = Math.Max(
                    carriedTough,
                    StateFields.Modified(
                        cast.World, next, "toughness",
                        cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
                return;
            }
            if ((card.Area.Type is DeckType.EngagedEnemiesArea
                    or DeckType.AlliesArea
                    || engagement.ContainsKey(cardId))
                && Current(cardId) >= tracedHealth)
            {
                LeavePlay(cardId);
            }
            int eliminated = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Where(pair => pair.seat.IdentityCard.ObjectId == cardId)
                .Select(pair => pair.player)
                .DefaultIfEmpty(-1)
                .First();
            if (eliminated >= 0 && Current(cardId) >= tracedHealth)
            {
                var plan = PlanTracePlayerElimination(
                    eliminated, cast, discarded, engagement);
                foreach (int relocated in plan.RelocatedCards)
                {
                    engagement[relocated] = plan.NextPlayer!.Value;
                }
                foreach (int leaving in plan.Leaving)
                {
                    discarded.Add(leaving);
                    if (Statuses.Count(
                            cast.World, cast.World.Cards[leaving],
                            Statuses.Tough) > 0)
                    {
                        tough[leaving] = 0;
                    }
                    else
                    {
                        tough.Remove(leaving);
                    }
                    TraceStatusesLeave(
                        leaving, cast, statusCounts, statusChanges);
                    engagement.Remove(leaving);
                }
            }
            if (eliminated == firstPlayer
                && Current(cardId) >= tracedHealth)
            {
                for (int offset = 1; offset < cast.World.Seats.Count; offset++)
                {
                    int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                    var identity = cast.World.Seats[candidate].IdentityCard;
                    long candidateHealth = SaturatingAdd(
                        TraceHealth(identity, discarded, threat, cast),
                        HealthBonus(identity.ObjectId));
                    if (Current(identity.ObjectId) < candidateHealth)
                    {
                        firstPlayer = candidate;
                        break;
                    }
                }
            }
        }

        foreach (var transfer in trace)
        {
            if (finished)
            {
                break;
            }

            int from = transfer.FromVillain is { } fromSelector
                ? TraceSelectorIncludesCard(
                    fromSelector, transfer.From, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                    is int tracedFrom ? tracedFrom
                    : int.MinValue
                : transfer.From;
            int to = transfer.ToVillain is { } toSelector
                ? TraceSelectorIncludesCard(
                    toSelector, transfer.To, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                    is int tracedTo ? tracedTo
                    : int.MinValue
                : transfer.To;
            if (from == int.MinValue || to == int.MinValue)
            {
                continue;
            }
            if (transfer.ChangesForm >= 0)
            {
                int seat = transfer.ChangesForm;
                ulong bit = PlayerSeat(seat);
                bool destinationIsCurrent = Forms.In(
                    cast.World, cast.World.Seats[seat], cast.World.Facts,
                    transfer.Form!);
                formsMayChange = destinationIsCurrent
                    ? formsMayChange & ~bit
                    : formsMayChange | bit;
                continue;
            }
            if (transfer.EntersPlay)
            {
                if (!discarded.Remove(to))
                {
                    // A repeated frame is reconstructed from the unchanged
                    // World, so its branch may still contain the same entry.
                    // Once trace-local play has made the card available, that
                    // later transfer is a no-op and must not migrate its
                    // engagement or restore its printed statuses.
                    continue;
                }
                engagement[to] = Resolver(cast);
                int enteredTough = Math.Max(
                    CurrentTough(to),
                    StateFields.Modified(
                        cast.World, cast.World.Cards[to], "toughness",
                        cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
                int liveTough = Statuses.Count(
                    cast.World, cast.World.Cards[to], Statuses.Tough);
                if (enteredTough == liveTough)
                {
                    tough.Remove(to);
                }
                else
                {
                    tough[to] = enteredTough;
                }
                TraceSetStatusCount(
                    cast.World.Cards[to], Statuses.Tough, enteredTough, cast,
                    statusCounts, statusChanges);
                continue;
            }
            if (transfer.RemovesThreat || transfer.PlacesThreat)
            {
                long current = CurrentThreat(to);
                long changed = transfer.PlacesThreat
                    ? SaturatingSum(current, [transfer.Amount])
                    : Math.Max(0, current - transfer.Amount);
                threat[to] = changed;
                if (transfer.RemovesThreat && changed == 0
                    && cast.World.Cards[to].Area.Type
                        == DeckType.SideSchemesArea)
                {
                    discarded.Add(to);
                }
                continue;
            }
            if (transfer.Discards)
            {
                LeavePlay(to);
                continue;
            }
            if (transfer.GrantsHealth)
            {
                health[to] = SaturatingSum(
                    HealthBonus(to), [transfer.Amount]);
                ObserveTarget();
                continue;
            }
            if (transfer.GrantsTrait is { } gainedTrait)
            {
                if (!traits.TryGetValue(to, out var gained))
                {
                    gained = new HashSet<string>(StringComparer.Ordinal);
                    traits[to] = gained;
                }
                gained.Add(gainedTrait);
                continue;
            }
            if (transfer.GrantsField is { } gainedField)
            {
                var key = (to, gainedField);
                long changed = SaturatingSum(
                    modifiers.GetValueOrDefault(key), [transfer.Amount]);
                if (changed == 0)
                {
                    modifiers.Remove(key);
                }
                else
                {
                    modifiers[key] = changed;
                }
                continue;
            }
            if (transfer.GrantsStatus is { } grantedStatus)
            {
                if (discarded.Contains(to))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' would give a status to a card "
                        + "that is not there");
                }
                var statusTarget = cast.World.Cards[to];
                if (grantedStatus == Statuses.Tough)
                {
                    int toughCurrent = CurrentTough(to);
                    if (toughCurrent >= 1)
                    {
                        continue;
                    }
                    int toughLive = Statuses.Count(
                        cast.World, statusTarget, Statuses.Tough);
                    if (toughLive == 1)
                    {
                        tough.Remove(to);
                    }
                    else
                    {
                        tough[to] = 1;
                    }
                    TraceSetStatusCount(
                        statusTarget, Statuses.Tough, 1, cast,
                        statusCounts, statusChanges);
                    continue;
                }
                var key = (to, grantedStatus);
                int live = Statuses.Count(
                    cast.World, statusTarget, grantedStatus);
                int current = statusCounts.GetValueOrDefault(key, live);
                int limit = TraceStatusLimit(
                    statusTarget, grantedStatus, cast, discarded, modifiers);
                if (current >= limit)
                {
                    continue;
                }
                int changed = current + 1;
                TraceSetStatusCount(
                    statusTarget, grantedStatus, changed, cast,
                    statusCounts, statusChanges);
                if (TraceStatusMakesVulnerable(
                    statusTarget, grantedStatus, changed, limit,
                    cast, discarded, modifiers))
                {
                    LeavePlay(to);
                }
                continue;
            }
            if (from < 0)
            {
                if (transfer.DealsDamage
                    && !CanTakeDamageInTrace(
                        cast, cast.World.Cards[to], discarded))
                {
                    continue;
                }
                long incoming = transfer.DealsDamage
                    ? AfterForcedDamageReplacements(
                        cast, to, transfer.Amount, damage, discarded,
                        currentVillain)
                    : transfer.Amount;
                var assignment = DamageAssignment.AfterReplacement(
                    incoming, incoming > 0 && CurrentTough(to) > 0);
                if (assignment.SpendsTough)
                {
                    tough[to] = CurrentTough(to) - 1;
                    continue;
                }
                damage[to] = SaturatingSum(Current(to), [assignment.Taken]);
                ResolveCharacterDefeat(to);
                ObserveTarget();
                continue;
            }

            long available = Current(from);
            long amount = Math.Min(available, transfer.Amount);
            if (to >= 0 && transfer.DealsDamage
                && !CanTakeDamageInTrace(
                    cast, cast.World.Cards[to], discarded))
            {
                continue;
            }
            damage[from] = available - amount;
            long landed = to >= 0 && transfer.DealsDamage
                ? AfterForcedDamageReplacements(
                    cast, to, amount, damage, discarded, currentVillain)
                : amount;
            if (to >= 0)
            {
                var assignment = DamageAssignment.AfterReplacement(
                    landed, landed > 0 && CurrentTough(to) > 0);
                if (assignment.SpendsTough)
                {
                    tough[to] = CurrentTough(to) - 1;
                }
                else
                {
                    damage[to] = SaturatingSum(Current(to), [assignment.Taken]);
                    ResolveCharacterDefeat(to);
                }
            }
            ObserveTarget();
        }
        return new DamageTraceState(
            damage, peak, state.Players, tough, statusChanges,
            statusCounts, health, discarded, threat,
            traits, modifiers, engagement,
            formsMayChange, firstPlayer,
            currentVillain, villainStagesDrawn, finished);
    }

    private static long AfterForcedDamageReplacements(
        AbilityAdmissionScope cast, int target, long amount, Dictionary<int, long> damage,
        HashSet<int> discarded, int currentVillain)
    {
        if (amount <= 0)
        {
            return amount;
        }

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        foreach (var area in cast.World.Areas.Where(area =>
            area.Host == target
            || target == currentVillain && area.Host == boardVillain
                && currentVillain >= 0 && boardVillain >= 0
                && string.Equals(
                    cast.World.Facts.Title(cast.World.Cards[currentVillain].FaceId),
                    cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                    StringComparison.Ordinal)))
        {
            foreach (var attachment in area.Cards.Where(card =>
                !discarded.Contains(card.ObjectId)))
            {
                var replacement = cast.Context.Program.On(attachment.FaceId).FirstOrDefault(ability =>
                    ability.Trigger.Timing == AbilityType.ForcedInterrupt
                    && ContainsEffect(ability.Effect, "soakDamage"));
                if (replacement is null)
                {
                    continue;
                }

                long placed = SaturatingSum(
                    damage.TryGetValue(attachment.ObjectId, out long traced)
                        ? traced
                        : attachment.Damage,
                    [amount]);
                damage[attachment.ObjectId] = placed;
                long threshold = SoakDiscardThreshold(replacement.Effect);
                if (threshold > 0 && placed >= threshold)
                {
                    discarded.Add(attachment.ObjectId);
                }
                return 0;
            }
        }
        return amount;
    }

    internal static bool ContainsEffect(AbilityEffect node, string kind) =>
        node.OperationName() == kind || MutationChildren(node).Any(child =>
            ContainsEffect(child, kind));

    internal static long SoakDiscardThreshold(AbilityEffect node)
    {
        if (node.OperationName() == "if"
            && node is AbilityEffect.Conditional
            {
                Test: AbilityCondition.AtLeast
                { Value: AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Damage } } comparison,
            }
            && ConditionalBranch(node, "then") is { } then
            && ContainsEffect(then, "discard"))
        {
            return comparison.Count is AbilityNumber.Constant constant
                ? constant.Value
                : throw new AbilityException("Soak discard threshold must be a constant number");
        }
        return MutationChildren(node)
            .Select(SoakDiscardThreshold)
            .FirstOrDefault(threshold => threshold > 0);
    }

    private static bool CanTakeDamageInTrace(
        AbilityAdmissionScope cast, Card target, HashSet<int> discarded)
    {
        if (discarded.Contains(target.ObjectId))
        {
            return false;
        }
        foreach (var ability in cast.Context.Program.On(target.FaceId).Where(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            var constant = cast.ForConstant(target);
            if (ProhibitsDamageInTrace(
                ability.Effect, constant, cast.Source, discarded))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ProhibitsDamageInTrace(
        AbilityEffect effect, AbilityAdmissionScope cast, Card source,
        HashSet<int> discarded) => effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
                ProhibitsDamageInTrace(step, cast, source, discarded)),
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
                ProhibitsDamageInTrace(step, cast, source, discarded)),
            AbilityEffect.Conditional conditional =>
                (TraceTest(conditional.Test, cast, discarded) ? conditional.Then : conditional.Else)
                is { } branch && ProhibitsDamageInTrace(
                    branch, cast, source, discarded),
            AbilityEffect.PreventDamageFrom prohibition => cast.World.Facts.Kind(source.FaceId)
                    == prohibition.SourceKind
                && Rules.State.Traits.Has(
                    cast.World, source, prohibition.SourceTrait,
                    cast.World.Facts),
            AbilityEffect.PreventDamageWhile prohibition => TraceTest(
                prohibition.Condition, cast, discarded),
            _ => false,
        };

    private static bool TraceTest(
        AbilityCondition condition, AbilityAdmissionScope cast, HashSet<int> discarded) => condition switch
        {
            AbilityCondition.All all => all.Operands.All(test =>
                TraceTest(test, cast, discarded)),
            AbilityCondition.Any any => any.Operands.Any(test =>
                TraceTest(test, cast, discarded)),
            AbilityCondition.Negated negated => !TraceTest(negated.Operand, cast, discarded),
            AbilityCondition.Exists exists => Every(exists.Cards, cast).Any(card =>
                !discarded.Contains(card.ObjectId)),
            AbilityCondition.TitleInPlay title => cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Any(card => !discarded.Contains(card.ObjectId)
                    && string.Equals(
                        cast.World.Facts.Title(card.FaceId),
                        title.Title, StringComparison.Ordinal)),
            _ => Test(condition, cast),
        };

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> DamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding)
    {
        if (node.OperationName() == "forEach")
        {
            long count = ForEachCount(node, cast);
            if (AmountMayChange(ForEachOf(node, cast).Count))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a for-each count that can change "
                    + "between traced iterations");
            }
            if (count == 0)
            {
                return [[]];
            }
            if (ContainsMutableAmount(EffectBody(node), cast))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a for-each amount that can change "
                    + "between traced iterations");
            }

            var effect = EffectBody(node);
            var iteration = DamageTraces(effect, cast, assumed, binding);
            if (!Choices(effect).Any()
                && effect.OperationName() is "dealDamage" or "removeThreat")
            {
                return
                [
                    .. iteration.Select(trace =>
                        (IReadOnlyList<DamageTransfer>)[
                            .. trace.Select(transfer => transfer with
                            {
                                Amount = SaturatingMultiply(transfer.Amount, count),
                            }),
                        ]),
                ];
            }

            IReadOnlyList<IReadOnlyList<DamageTransfer>> repeated = [[]];
            for (long frame = 0; frame < count; frame++)
            {
                repeated =
                [
                    .. repeated.SelectMany(prefix => iteration.Select(suffix =>
                        (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix])),
                ];
            }
            return repeated;
        }

        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            var branches = RepeatedTestCanChange(test, assumed)
                    || binding && BindingCanChange(test)
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            // The engine represents a skipped branch as one empty trace.
            // Returning no traces would erase the preceding players' effects
            // when this frame is composed with the rest of the sequence.
            var branchTraces = branches.SelectMany(branch => DamageTraces(
                branch, cast, assumed, binding)).ToList();
            return branchTraces.Count == 0 ? [[]] : branchTraces;
        }

        if (node.OperationName() == "choose")
        {
            return
            [
                .. MutationChildren(node).SelectMany(child =>
                    DamageTraces(child, cast, assumed, binding)),
            ];
        }

        var own = DamageTransfers(node, cast);
        var children = MutationChildren(node).ToList();
        IReadOnlyList<IReadOnlyList<DamageTransfer>> traces = [own];
        foreach (var child in children)
        {
            var next = DamageTraces(child, cast, assumed, binding);
            traces =
            [
                .. traces.SelectMany(prefix => next.Select(suffix =>
                    (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix])),
            ];
        }
        return traces;
    }

    private static IReadOnlyList<DamageTransfer> DamageTransfers(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() == "changeForm")
        {
            var change = FormChangeOf(node, cast);
            return [new DamageTransfer(
                0, 0, 0,
                ChangesForm: Seat(change.Player, cast),
                Form: change.Form)];
        }
        if (node.OperationName() is "dealDamage" or "indirectDamage")
        {
            return
            [
                .. TraceCards(DamageSelectionOf(node, cast), cast).Select(target =>
                    new DamageTransfer(
                        -1, target.Card.ObjectId,
                        Amount(DamageAmountOf(node, cast), cast),
                        DealsDamage: true,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.OperationName() == "replaceThreatWithDamage"
            && TraceCardNamed(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } replaced)
        {
            return [new DamageTransfer(
                -1, replaced.Card.ObjectId,
                cast.Occurrence.Threat?.Remaining ?? long.MaxValue,
                DealsDamage: true,
                ToVillain: replaced.VillainSelector)];
        }
        if (node.OperationName() == "heal"
            && TraceCardNamed(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast) is { } healed)
        {
            return [new DamageTransfer(
                healed.Card.ObjectId, -1, Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast),
                FromVillain: healed.VillainSelector)];
        }
        if (node.OperationName() == "moveDamage"
            && TraceCardNamed(EffectOf<AbilityEffect.MoveDamage>(node, cast).From, cast) is { } from
            && TraceCardNamed(EffectOf<AbilityEffect.MoveDamage>(node, cast).To, cast) is { } to)
        {
            return [new DamageTransfer(
                from.Card.ObjectId, to.Card.ObjectId,
                Amount(EffectOf<AbilityEffect.MoveDamage>(node, cast).Amount, cast),
                DealsDamage: true,
                FromVillain: from.VillainSelector,
                ToVillain: to.VillainSelector)];
        }
        if (node.OperationName() == "giveStatus")
        {
            return
            [
                .. TraceCards(EffectOf<AbilityEffect.GiveStatus>(node, cast).Cards, cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0,
                        GrantsStatus: EffectOf<AbilityEffect.GiveStatus>(node, cast).Status,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.OperationName() == "grantUntil"
            && EffectOf<AbilityEffect>(node, cast) is AbilityEffect.GrantTrait trait)
        {
            return
            [
                .. TraceCards(trait.Cards, cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0,
                        GrantsTrait: trait.Trait,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.OperationName() == "grantUntil"
            && EffectOf<AbilityEffect>(node, cast) is AbilityEffect.GrantField { Field: not "health" } field
            && TraceCards(field.Cards, cast) is { Count: > 0 } targets)
        {
            return
            [
                .. targets.Select(target => new DamageTransfer(
                    0, target.Card.ObjectId,
                    Amount(field.Amount, cast),
                    GrantsField: field.Field,
                    ToVillain: target.VillainSelector)),
            ];
        }
        if (node.OperationName() == "grantUntil"
            && EffectOf<AbilityEffect>(node, cast) is AbilityEffect.GrantField { Field: "health" } health
            && TraceCardNamed(health.Cards, cast) is { } healthier)
        {
            return [new DamageTransfer(
                0, healthier.Card.ObjectId,
                Amount(health.Amount, cast),
                GrantsHealth: true,
                ToVillain: healthier.VillainSelector)];
        }
        if (node.OperationName() == "discard")
        {
            var cards = EffectOf<AbilityEffect.CardAction>(node, cast).Selection;
            return
            [
                .. TraceCards(cards, cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0, Discards: true,
                        ToVillain: target.VillainSelector)),
            ];
        }
        if (node.OperationName() == "putIntoPlay"
            && TraceCardNamed(EffectOf<AbilityEffect.PutIntoPlay>(node, cast).Card, cast) is { } entering)
        {
            if (cast.Context.Program.On(entering.Card.FaceId).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
            {
                // The card is intentionally not moved while eligibility is
                // traced, so its constant abilities are not active in the
                // unchanged World. Refuse that continuation before mutation
                // rather than rank later selectors against a plausible lie.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' puts '{entering.Card.FaceId}' into play "
                    + "before a repeated continuation reads its constant abilities, "
                    + "which is not implemented");
            }
            return [new DamageTransfer(
                0, entering.Card.ObjectId, 0, EntersPlay: true)];
        }
        if (node.OperationName() is "removeThreat" or "placeThreat")
        {
            bool removes = node.OperationName() == "removeThreat";
            return
            [
                .. Every(ThreatSelectionOf(node, cast), cast).Select(scheme =>
                    new DamageTransfer(
                        0, scheme.ObjectId,
                        Amount(removes ? EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount
                            : EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount, cast),
                        RemovesThreat: removes,
                        PlacesThreat: !removes)),
            ];
        }
        return [];
    }

    private static List<TraceCard> TraceCards(
        AbilityCardSelection value, AbilityAdmissionScope cast)
    {
        bool dynamic = SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast);
        var selected = dynamic
            ? TraceCandidateCards(value, cast)
            : Every(value, cast).ToList();
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        var traced = selected.Select(card => new TraceCard(
            card, dynamic ? value : VillainSelector(value, card, cast))).ToList();
        if (current is not null
            && selected.All(card => card.ObjectId != current.ObjectId)
            && dynamic)
        {
            traced.Insert(0, new TraceCard(current, value));
        }
        return traced;
    }

    private static TraceCard? TraceCardNamed(
        AbilityCardSelection value, AbilityAdmissionScope cast)
    {
        if (Find(value, cast) is { } found)
        {
            return new TraceCard(found, VillainSelector(value, found, cast));
        }
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        return current is not null && PotentialVillainSelector(value, cast)
            ? new TraceCard(current, value)
            : null;
    }

    private static AbilityCardSelection? VillainSelector(
        AbilityCardSelection value, Card resolved, AbilityAdmissionScope cast)
    {
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        if (current is null || resolved.ObjectId != current.ObjectId
            || value is AbilityCardSelection.Bound)
        {
            return null;
        }
        return SelectorCanTrackVillain(value, current, cast) ? value : null;
    }

    private static bool SelectorCanTrackVillain(
        AbilityCardSelection selector, Card current, AbilityAdmissionScope cast) => selector switch
    {
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.Villain
            or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies or AbilityCardQuery.Characters,
        AbilityCardSelection.Titled titled => string.Equals(
            titled.Title, cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal),
        AbilityCardSelection.EnemiesWithTrait trait => TraceHasTrait(current, trait.Trait, cast, []),
        AbilityCardSelection.WithTrait trait => TraceHasTrait(current, trait.Trait, cast, [])
            && SelectorCanTrackVillain(trait.Cards, current, cast),
        AbilityCardSelection.WithoutAnotherCopyAttached other => SelectorCanTrackVillain(other.Cards, current, cast),
        AbilityCardSelection.Ranked ranked => SelectorCanTrackVillain(ranked.Cards, current, cast),
        _ => false,
    };

    private static int? TraceSelectorIncludesCard(
        AbilityCardSelection value, int bound, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        int candidateId = bound == boardVillain ? currentVillain : bound;
        if (candidateId < 0 || discarded.Contains(candidateId))
        {
            return null;
        }
        var candidate = cast.World.Cards[candidateId];
        return TraceSelectorMatches(
            value, candidate, currentVillain, cast, discarded, traits, modifiers,
            engagement)
            ? candidateId
            : null;
    }

    private static bool TraceQueryMatches(
        AbilityCardQuery query, Card candidate, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        bool villain = candidate.ObjectId == currentVillain;
        var kind = FacedownDrones.Kind(candidate, cast.World.Facts);
        return query switch
        {
            AbilityCardQuery.Villain => villain,
            AbilityCardQuery.Enemies => villain
                || kind == CardKind.Minion,
            AbilityCardQuery.Minions => kind == CardKind.Minion,
            AbilityCardQuery.Characters => villain
                || kind is CardKind.Minion or CardKind.Hero
                    or CardKind.AlterEgo or CardKind.Ally,
            AbilityCardQuery.AttackableEnemies => villain
                ? VillainIsAttackableInTrace(
                    cast, candidate, discarded, modifiers, engagement)
                : kind == CardKind.Minion
                    && CanTakeDamageInTrace(cast, candidate, discarded),
            AbilityCardQuery.MinionsEngagedWithYou =>
                kind == CardKind.Minion
                && TraceEngagedWith(candidate, cast.Player, engagement),
            AbilityCardQuery.DronesEngagedWithYou =>
                kind == CardKind.Minion
                && TraceHasTrait(candidate, "DRONE", cast, discarded, traits)
                && TraceEngagedWith(candidate, Resolver(cast), engagement),
            AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
                kind == CardKind.Minion
                && cast.Chosen is { Owner: >= 0 } chosen
                && TraceEngagedWith(candidate, chosen.Owner, engagement),
            AbilityCardQuery.UpgradesYouControl =>
                candidate.Area.Type == DeckType.UpgradesArea
                && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
            AbilityCardQuery.SupportsYouControl =>
                candidate.Area.Type == DeckType.SupportsArea
                && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
            AbilityCardQuery.UpgradesAndSupportsYouControl =>
                candidate.Area.Type is DeckType.UpgradesArea
                    or DeckType.SupportsArea
                && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
            _ => QueryCards(query, cast).Any(card =>
                card.ObjectId == candidate.ObjectId),
        };
    }

    private static bool TraceEngagedWith(
        Card card, int player, Dictionary<int, int> engagement) =>
        engagement.TryGetValue(card.ObjectId, out int traced)
            ? traced == player
            : card.Area.Type == DeckType.EngagedEnemiesArea
                && card.Area.PlayArea == PlayArea.Of(player);

    private static int TracePlayAreaPlayer(
        Card card, Dictionary<int, int> placement) =>
        placement.TryGetValue(card.ObjectId, out int traced)
            ? traced
            : card.Area.PlayArea.Player;

    private static bool VillainIsAttackableInTrace(
        AbilityAdmissionScope cast, Card current, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int player = Resolver(cast);
        bool guarded = cast.World
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player))
            .Cards.Any(enemy => !discarded.Contains(enemy.ObjectId)
                && !engagement.ContainsKey(enemy.ObjectId)
                && FacedownDrones.Kind(enemy, cast.World.Facts) == CardKind.Minion
                && TraceModified(
                    enemy, "guard", cast, discarded, modifiers) > 0)
            || engagement.Any(pair => pair.Value == player
                && !discarded.Contains(pair.Key)
                && TraceModified(
                    cast.World.Cards[pair.Key], "guard",
                    cast, discarded, modifiers) > 0);
        return !guarded && CanTakeDamageInTrace(cast, current, discarded);
    }

    private static bool TraceHasTrait(
        Card current, string trait, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, HashSet<string>>? traits = null)
    {
        if (traits?.TryGetValue(current.ObjectId, out var gained) == true
            && gained.Contains(trait))
        {
            return true;
        }
        if (FacedownDrones.InherentTraits(current, cast.World.Facts)
            .Contains(trait, StringComparer.Ordinal))
        {
            return true;
        }

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        string grantedKind = Rules.State.Traits.Granted + trait;
        if (cast.World.Effects.Active().Any(effect =>
            effect.Affects == current.ObjectId
            && (effect.Source != EffectSource.ConstantAbility
                || effect.Card is not int source || !discarded.Contains(source))
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal)))
        {
            return true;
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        return cast.World.Effects.Active().Any(effect =>
            effect.Affects == boardVillain
            && effect.Card is int source && carriedIds.Contains(source)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));
    }

    private static List<Card> TraceCarriedAttachments(
        Card current, AbilityAdmissionScope cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (boardVillain < 0 || !string.Equals(
                cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal))
        {
            return [];
        }
        return
        [
            .. cast.World.Areas
                .Where(area => area.Host == boardVillain
                    || area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => !discarded.Contains(card.ObjectId)),
        ];
    }

    private static long TraceModified(
        Card current, string field, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long>? modifiers = null)
    {
        if (StateFields.FilledFrom.TryGetValue(field, out string? attribute)
            && attribute is "ATK" or "THW" or "DEF" or "REC" or "SCH"
            && !StateFields.HasUsablePrintedPower(
                cast.World.Facts, current.FaceId, attribute))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            // Match StateFields.Modified's early return before applying either
            // trace-local or carried modifiers.
            return 0;
        }

        string? printed = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        long value = StateFields.Modified(
                cast.World, current, field, cast.World.Facts, cast.World.Players)
            + (modifiers?.GetValueOrDefault((current.ObjectId, field)) ?? 0);
        if (printed is not null)
        {
            value -= cast.World.Areas
                .Where(area => area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => discarded.Contains(card.ObjectId))
                .Sum(card => cast.World.Facts.PrintedValue(
                    card.FaceId, printed, cast.World.Players));
        }
        value -= cast.World.Effects.Active()
            .Where(effect => string.Equals(
                    effect.Kind, field, StringComparison.Ordinal)
                && effect.AppliesTo(cast.World, current)
                && effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source && discarded.Contains(source))
            .Sum(effect => effect.Amount);

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (current.ObjectId == boardVillain)
        {
            return value;
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        if (printed is not null)
        {
            value += carried.Sum(card => cast.World.Facts.PrintedValue(
                card.FaceId, printed, cast.World.Players));
        }

        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        value += cast.World.Effects.Active()
            .Where(effect => effect.Affects == boardVillain
                && effect.Card is int source && carriedIds.Contains(source)
                && string.Equals(effect.Kind, field, StringComparison.Ordinal))
            .Sum(effect => effect.Amount);
        return value;
    }

    private static int TraceStatusLimit(
        Card card, string status, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers)
    {
        if (status is not (Statuses.Stunned or Statuses.Confused))
        {
            return 1;
        }
        if (TraceModified(card, "stalwart", cast, discarded, modifiers) > 0)
        {
            return 0;
        }
        return TraceModified(card, "steady", cast, discarded, modifiers) > 0
            ? 2
            : 1;
    }

    private static void TraceStatusesLeave(
        int cardId, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var statuses = statusCounts.Keys
            .Where(key => key.Card == cardId)
            .Select(key => key.Status)
            .Concat(cast.World.Areas
                .Where(area => area.Type == DeckType.StatusArea
                    && area.Host == cardId)
                .SelectMany(area => area.Cards)
                .Select(card => card.FaceId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (string status in statuses)
        {
            TraceSetStatusCount(
                cast.World.Cards[cardId], status, 0, cast,
                statusCounts, statusChanges);
        }
    }

    private static void TraceSetStatusCount(
        Card card, string status, int count, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var key = (card.ObjectId, status);
        int live = Statuses.Count(cast.World, card, status);
        if (count == live)
        {
            statusCounts.Remove(key);
        }
        else
        {
            statusCounts[key] = count;
        }
        if ((count > 0) == (live > 0))
        {
            statusChanges.Remove(key);
        }
        else
        {
            statusChanges.Add(key);
        }
    }

    private static bool TraceStatusMakesVulnerable(
        Card card, string status, int count, int limit, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers) =>
        status is Statuses.Stunned or Statuses.Confused
        && limit > 0
        && count >= limit
        && TraceModified(
            card, "vulnerable", cast, discarded, modifiers) > 0;

    private static bool AnotherCopyAttachedInTrace(
        Card current, AbilityAdmissionScope cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        string sourceTitle = cast.World.Facts.Title(cast.Source.FaceId);
        bool carried = boardVillain >= 0 && string.Equals(
            cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
            cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal);
        return cast.World.Areas
            .Where(area => area.Host == current.ObjectId
                || carried && area.Host == boardVillain)
            .SelectMany(area => area.Cards)
            .Any(attached => attached.ObjectId != cast.Source.ObjectId
                && !discarded.Contains(attached.ObjectId)
                && string.Equals(
                    cast.World.Facts.Title(attached.FaceId), sourceTitle,
                    StringComparison.Ordinal));
    }

    private static bool TraceRankedCandidatesInclude(
        List<Card> candidates, Card candidate, AbilityCardRank rank, bool maximum,
        AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers)
    {
        long Rank(Card card) => rank switch
        {
            AbilityCardRank.Cost => cast.World.Facts.PrintedValue(
                card.FaceId, "Cost", cast.World.Players),
            AbilityCardRank.Attack => TraceModified(
                card, "attack", cast, discarded, modifiers),
            AbilityCardRank.PrintedHealth => FacedownDrones.BaseValue(
                card, cast.World.Facts, "HP", cast.World.Players),
            _ => throw new InvalidOperationException("Unknown compiled rank in a projected selector"),
        };
        long extreme = maximum ? candidates.Max(Rank) : candidates.Min(Rank);
        return Rank(candidate) == extreme;
    }

    private static long MutationTotal(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding,
        long own,
        Func<AbilityEffect, long> childAmount)
    {
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            if (RepeatedTestCanChange(test, assumed)
                || binding && BindingCanChange(test))
            {
                long possible = ConditionalBranches((AbilityEffect.Conditional)node)
                    .Where(value => value is not null)
                    .Select(value => childAmount(value))
                    .DefaultIfEmpty(0)
                    .Max();
                return SaturatingSum(own, [possible]);
            }

            bool passes = Test(test, cast);
            long active = ConditionalBranch(node, passes ? "then" : "else") is { } branch
                ? childAmount(branch)
                : 0;
            return SaturatingSum(own, [active]);
        }

        var amounts = MutationChildren(node).Select(childAmount).ToList();

        // The engine chooses one option. Ordered and simultaneous children all
        // resolve, so only those amounts combine.
        long descendants = node.OperationName() switch
        {
            "choose" => amounts.DefaultIfEmpty(0).Max(),
            "forEach" => SaturatingMultiply(
                amounts.SingleOrDefault(), ForEachCount(node, cast)),
            _ => SaturatingSum(0, amounts),
        };
        return SaturatingSum(own, [descendants]);
    }

    internal static long SaturatingSum(long own, IEnumerable<long> rest)
        => AbilityAmounts.SaturatingSum(own, rest);

    internal static long SaturatingMultiply(long amount, long multiplier)
    {
        if (amount <= 0 || multiplier <= 0)
        {
            return 0;
        }
        return amount > long.MaxValue / multiplier
            ? long.MaxValue
            : amount * multiplier;
    }

    private static bool CanExhaust(
        long amountPerFrame, int frames, long remaining) =>
        amountPerFrame > 0 && frames > 0
        && amountPerFrame >= (remaining + frames - 1) / frames;

    internal static IEnumerable<AbilityEffect> MutationChildren(AbilityEffect node) =>
        node.OperationName() is "attack" or "thwart"
            ? [EffectBody(node)]
            : ContinuationChildren(node);

    internal static IEnumerable<AbilityEffect> ContinuationChildren(AbilityEffect node) =>
        node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "chooseCard" or "eachPlayer" or "forEach" =>
                [EffectBody(node)],
            "eachTime" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "afterActivation" => [EffectBody(node)],
            "payOrEffect" or "payOrExhaust" => [EffectFollowing(node)],
            "thwartSchemes" or "thwartDifferentSchemes" or "legalPractice" =>
                [((AbilityEffect.ThwartGroup)node).Thwart],
            _ => ResolutionChildren(node),
        };

    /// <summary>Whether this player-card effect can remove any threat.</summary>
    private static bool CanRemoveThreat(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var scheme = Find(ThreatSelectionOf(node, cast), cast);
        return scheme is not null
            && scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
            && CanRemoveThreatFrom(node, cast, scheme);
    }

    private static bool CanRemoveThreatFrom(AbilityEffect node, AbilityAdmissionScope cast, Card scheme) =>
        AbilityProgramQueries.CanRemoveThreat(
            cast.World, cast.Context.Program, scheme,
            OverriddenThreatRemovalSource(node, cast))
        && (IgnoresCrisis(node, cast)
            || scheme.Area.Type != DeckType.MainSchemesArea
            || !IsPlayerCard(cast)
            || !MainScheme.Crisis(cast.World, cast.World.Facts));

    private static int OverriddenThreatRemovalSource(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.RemoveThreat>(node, cast).OverridesCannotFrom is { } source
            ? Find(source, cast)?.ObjectId ?? -1
            : -1;

    private static bool IgnoresCrisis(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.RemoveThreat>(node, cast).IgnoresCrisis;

    /// <summary>Whether at least one named player can draw a card.</summary>
    private static bool CanDraw(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.Draw>(node, cast) is var draw
        && draw.Count > 0
        && Seats(draw.Players, cast).Any(player =>
            CanDraw(cast.World, player));

    internal static bool CanDraw(World world, int player) =>
        world.Seats[player].Deck.Cards.Count > 0;

    /// <summary>Whether a search names at least one searchable game area.</summary>
    private static bool HasSearchableArea(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var search = EffectOf<AbilityEffect.Search>(node, cast);
        if (search.Areas.IsEmpty)
        {
            return false;
        }

        var searchedAreas = search.Areas
            .Select(value => Area(value, cast))
            .ToList();

        var searched = SearchAreaTypes(node, cast);
        if (cast.Reachability.CheckingInitiation
            && (cast.Reachability.PriorSteps.Any(step =>
                    MayChangeAnyArea(step, searched, cast))
                || cast.Reachability.PaymentCost is { } cost
                    && CostMayChangeAnyArea(cost, searched, cast)))
        {
            if (cast.Reachability.FilteringContinuationOption)
            {
                return false;
            }
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searches an area after its matching "
                + "cards may change");
        }

        string wanted = search.Face;
        int matches = searchedAreas.SelectMany(area => area.Cards)
            .Count(card => string.Equals(
                card.FaceId, wanted, StringComparison.Ordinal));
        if (matches > 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {matches} copies of "
                + $"'{wanted}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }
        return true;
    }

}
