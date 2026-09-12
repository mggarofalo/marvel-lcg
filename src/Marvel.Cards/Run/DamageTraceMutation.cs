using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;

namespace Marvel.Cards.Run;

internal sealed class DamageTraceMutation
{
    private readonly DamageTraceState original;
    private readonly Card target;
    private readonly AbilityAdmissionScope cast;
    private readonly Dictionary<int, long> damage;
    private readonly Dictionary<int, int> tough;
    private readonly HashSet<(int Card, string Status)> statusChanges;
    private readonly Dictionary<(int Card, string Status), int> statusCounts;
    private readonly Dictionary<int, long> health;
    private readonly HashSet<int> discarded;
    private readonly Dictionary<int, long> threat;
    private readonly Dictionary<int, HashSet<string>> traits;
    private readonly Dictionary<(int Card, string Field), long> modifiers;
    private readonly Dictionary<int, int> engagement;
    private ulong formsMayChange;
    private int firstPlayer;
    private int currentVillain;
    private int villainStagesDrawn;
    private bool finished;
    private long peak;

    internal DamageTraceMutation(
        DamageTraceState state, Card target, AbilityAdmissionScope cast)
    {
        original = state;
        this.target = target;
        this.cast = cast;
        damage = new(state.Damage);
        tough = new(state.Tough);
        statusChanges = new(state.StatusChanges);
        statusCounts = new(state.StatusCounts);
        health = new(state.Health);
        discarded = new(state.Discarded);
        threat = new(state.Threat);
        traits = state.Traits.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        modifiers = new(state.Modifiers);
        engagement = new(state.Engagement);
        formsMayChange = state.FormsMayChange;
        firstPlayer = state.FirstPlayer;
        currentVillain = state.CurrentVillain;
        villainStagesDrawn = state.VillainStagesDrawn;
        finished = state.Finished;
        peak = state.PeakTargetPressure;
    }

    internal DamageTraceState Apply(IReadOnlyList<DamageTransfer> trace)
    {
        foreach (var transfer in trace)
        {
            if (finished) break;
            Apply(transfer);
        }
        return new DamageTraceState(
            damage, peak, original.Players, tough, statusChanges,
            statusCounts, health, discarded, threat, traits, modifiers,
            engagement, formsMayChange, firstPlayer, currentVillain,
            villainStagesDrawn, finished);
    }

    private void Apply(DamageTransfer transfer)
    {
        int from = ResolveCard(transfer.FromVillain, transfer.From);
        int to = ResolveCard(transfer.ToVillain, transfer.To);
        if (from == int.MinValue || to == int.MinValue) return;
        if (ApplyStructural(transfer, to)) return;
        if (ApplyGrant(transfer, to)) return;
        ApplyDamage(transfer, from, to);
    }

    private int ResolveCard(AbilityCardSelection? selector, int fallback) =>
        selector is null
            ? fallback
            : TraceSelectorIncludesCard(
                selector, fallback, currentVillain, cast,
                discarded, traits, modifiers, engagement) ?? int.MinValue;

    private bool ApplyStructural(DamageTransfer transfer, int to)
    {
        if (transfer.ChangesForm >= 0) ChangeForm(transfer);
        else if (transfer.EntersPlay) EnterPlay(to);
        else if (transfer.RemovesThreat || transfer.PlacesThreat)
            ChangeThreat(transfer, to);
        else if (transfer.Discards) LeavePlay(to);
        else return false;
        return true;
    }

    private void ChangeForm(DamageTransfer transfer)
    {
        int seat = transfer.ChangesForm;
        ulong bit = PlayerSeat(seat);
        bool destinationIsCurrent = Forms.In(
            cast.World, cast.World.Seats[seat], cast.World.Facts, transfer.Form!);
        formsMayChange = destinationIsCurrent
            ? formsMayChange & ~bit
            : formsMayChange | bit;
    }

    private void EnterPlay(int cardId)
    {
        if (!discarded.Remove(cardId)) return;
        engagement[cardId] = Resolver(cast);
        var card = cast.World.Cards[cardId];
        int entered = Math.Max(
            CurrentTough(cardId),
            StateFields.Modified(
                cast.World, card, "toughness",
                cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
        SetTough(card, entered);
    }

    private void ChangeThreat(DamageTransfer transfer, int cardId)
    {
        long current = CurrentThreat(cardId);
        long changed = transfer.PlacesThreat
            ? SaturatingSum(current, [transfer.Amount])
            : Math.Max(0, current - transfer.Amount);
        threat[cardId] = changed;
        if (transfer.RemovesThreat && changed == 0
            && cast.World.Cards[cardId].Area.Type == DeckType.SideSchemesArea)
        {
            discarded.Add(cardId);
        }
    }

    private bool ApplyGrant(DamageTransfer transfer, int to)
    {
        if (transfer.GrantsHealth) GrantHealth(to, transfer.Amount);
        else if (transfer.GrantsTrait is { } trait) GrantTrait(to, trait);
        else if (transfer.GrantsField is { } field)
            GrantField(to, field, transfer.Amount);
        else if (transfer.GrantsStatus is { } status) GrantStatus(to, status);
        else return false;
        return true;
    }

    private void GrantHealth(int cardId, long amount)
    {
        health[cardId] = SaturatingSum(HealthBonus(cardId), [amount]);
        ObserveTarget();
    }

    private void GrantTrait(int cardId, string trait)
    {
        if (!traits.TryGetValue(cardId, out var values))
        {
            values = new HashSet<string>(StringComparer.Ordinal);
            traits[cardId] = values;
        }
        values.Add(trait);
    }

    private void GrantField(int cardId, string field, long amount)
    {
        var key = (cardId, field);
        long changed = SaturatingSum(modifiers.GetValueOrDefault(key), [amount]);
        if (changed == 0) modifiers.Remove(key);
        else modifiers[key] = changed;
    }

    private void GrantStatus(int cardId, string status)
    {
        if (discarded.Contains(cardId))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would give a status to a card that is not there");
        }
        var card = cast.World.Cards[cardId];
        if (status == Statuses.Tough) GrantTough(card);
        else GrantLimitedStatus(card, status);
    }

    private void GrantTough(Card card)
    {
        if (CurrentTough(card.ObjectId) >= 1) return;
        SetTough(card, 1);
    }

    private void SetTough(Card card, int count)
    {
        int live = Statuses.Count(cast.World, card, Statuses.Tough);
        if (count == live) tough.Remove(card.ObjectId);
        else tough[card.ObjectId] = count;
        TraceSetStatusCount(
            card, Statuses.Tough, count, cast, statusCounts, statusChanges);
    }

    private void GrantLimitedStatus(Card card, string status)
    {
        var key = (card.ObjectId, status);
        int current = statusCounts.GetValueOrDefault(
            key, Statuses.Count(cast.World, card, status));
        int limit = TraceStatusLimit(card, status, cast, discarded, modifiers);
        if (current >= limit) return;
        int changed = current + 1;
        TraceSetStatusCount(
            card, status, changed, cast, statusCounts, statusChanges);
        if (TraceStatusMakesVulnerable(
            card, status, changed, limit, cast, discarded, modifiers))
        {
            LeavePlay(card.ObjectId);
        }
    }

    private long Current(int cardId) => damage.TryGetValue(cardId, out long amount)
        ? amount : cast.World.Cards[cardId].Damage;

    private int CurrentTough(int cardId) => tough.TryGetValue(cardId, out int count)
        ? count : Statuses.Count(cast.World, cast.World.Cards[cardId], Statuses.Tough);

    private long HealthBonus(int cardId) => health.GetValueOrDefault(cardId);

    private long CurrentThreat(int cardId) => threat.TryGetValue(cardId, out long amount)
        ? amount : cast.World.Cards[cardId].Tokens.GetValueOrDefault("k_threat");

    private void ObserveTarget() => peak = Math.Max(
        peak, Current(target.ObjectId) - HealthBonus(target.ObjectId));

    private void ApplyDamage(DamageTransfer transfer, int from, int to)
    {
        if (from < 0)
        {
            ApplyIncomingDamage(transfer, to);
            return;
        }
        ApplyMovedDamage(transfer, from, to);
    }

    private void ApplyIncomingDamage(DamageTransfer transfer, int to)
    {
        if (!CanLand(transfer, to)) return;
        long incoming = transfer.DealsDamage
            ? AfterForcedDamageReplacements(
                cast, to, transfer.Amount, damage, discarded, currentVillain)
            : transfer.Amount;
        AssignDamage(to, incoming);
        ObserveTarget();
    }

    private void ApplyMovedDamage(DamageTransfer transfer, int from, int to)
    {
        if (!CanLand(transfer, to)) return;
        long available = Current(from);
        long amount = Math.Min(available, transfer.Amount);
        damage[from] = available - amount;
        long landed = to >= 0 && transfer.DealsDamage
            ? AfterForcedDamageReplacements(
                cast, to, amount, damage, discarded, currentVillain)
            : amount;
        if (to >= 0) AssignDamage(to, landed);
        ObserveTarget();
    }

    private bool CanLand(DamageTransfer transfer, int to) =>
        to < 0
        || !transfer.DealsDamage
        || CanTakeDamageInTrace(cast, cast.World.Cards[to], discarded);

    private void AssignDamage(int cardId, long incoming)
    {
        var assignment = DamageAssignment.AfterReplacement(
            incoming, incoming > 0 && CurrentTough(cardId) > 0);
        if (assignment.SpendsTough)
        {
            tough[cardId] = CurrentTough(cardId) - 1;
            return;
        }
        damage[cardId] = SaturatingSum(Current(cardId), [assignment.Taken]);
        ResolveCharacterDefeat(cardId);
    }

    private void ResolveCharacterDefeat(int cardId)
    {
        var card = cast.World.Cards[cardId];
        long tracedHealth = SaturatingAdd(
            TraceHealth(card, discarded, threat, cast), HealthBonus(cardId));
        if (cardId == currentVillain && Current(cardId) >= tracedHealth)
        {
            ResolveVillainDefeat(card);
            return;
        }
        ResolveInPlayDefeat(card, tracedHealth);
        ResolveIdentityDefeat(card, tracedHealth);
    }

    private void ResolveVillainDefeat(Card card)
    {
        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - villainStagesDrawn;
        if (nextIndex < 0)
        {
            LeavePlay(card.ObjectId);
            finished = true;
            return;
        }
        var next = deck.Cards[nextIndex];
        bool carries = string.Equals(
            cast.World.Facts.Title(card.FaceId),
            cast.World.Facts.Title(next.FaceId), StringComparison.Ordinal);
        if (carries) discarded.Add(card.ObjectId);
        else LeavePlay(card.ObjectId);
        RefuseUntraceableVillainEntry(card, next);
        AdvanceVillain(card, next, carries);
    }

    private void RefuseUntraceableVillainEntry(Card current, Card next)
    {
        if (AbilityProgramQueries.On(cast.Context.Program, next).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "repeated continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (!HasLiveVillainRetargetingConstant(
            discarded, current, next, cast, threat, damage, modifiers, traits,
            [
                .. statusChanges,
                .. tough.Keys.Select(card => (card, Statuses.Tough)),
            ], engagement, formsMayChange, firstPlayer)) return;
        throw new RulesNotImplementedException(
            $"villain stage '{next.FaceId}' enters play before a "
            + "repeated continuation reads retargeting constant abilities, "
            + "which is not implemented");
    }

    private void AdvanceVillain(Card current, Card next, bool carries)
    {
        villainStagesDrawn++;
        int carriedTough = carries ? CurrentTough(current.ObjectId) : 0;
        currentVillain = next.ObjectId;
        damage[next.ObjectId] = 0;
        tough[next.ObjectId] = Math.Max(
            carriedTough,
            StateFields.Modified(
                cast.World, next, "toughness",
                cast.World.Facts, cast.World.Players) > 0 ? 1 : 0);
    }

    private void ResolveInPlayDefeat(Card card, long tracedHealth)
    {
        bool inPlay = card.Area.Type is DeckType.EngagedEnemiesArea
            or DeckType.AlliesArea || engagement.ContainsKey(card.ObjectId);
        if (inPlay && Current(card.ObjectId) >= tracedHealth)
        {
            LeavePlay(card.ObjectId);
        }
    }

    private void ResolveIdentityDefeat(Card card, long tracedHealth)
    {
        int player = PlayerForIdentity(card.ObjectId);
        if (player < 0 || Current(card.ObjectId) < tracedHealth) return;
        ApplyElimination(player);
        if (player == firstPlayer) AdvanceFirstPlayer();
    }

    private int PlayerForIdentity(int cardId) => cast.World.Seats
        .Select((seat, player) => (seat, player))
        .Where(pair => pair.seat.IdentityCard.ObjectId == cardId)
        .Select(pair => pair.player)
        .DefaultIfEmpty(-1)
        .First();

    private void ApplyElimination(int player)
    {
        var plan = PlanTracePlayerElimination(
            player, cast, discarded, engagement);
        foreach (int relocated in plan.RelocatedCards)
        {
            engagement[relocated] = plan.NextPlayer!.Value;
        }
        foreach (int leaving in plan.Leaving)
        {
            DiscardCard(leaving);
        }
    }

    private void AdvanceFirstPlayer()
    {
        for (int offset = 1; offset < cast.World.Seats.Count; offset++)
        {
            int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
            var identity = cast.World.Seats[candidate].IdentityCard;
            long candidateHealth = SaturatingAdd(
                TraceHealth(identity, discarded, threat, cast),
                HealthBonus(identity.ObjectId));
            if (Current(identity.ObjectId) >= candidateHealth) continue;
            firstPlayer = candidate;
            return;
        }
    }

    private void LeavePlay(int cardId)
    {
        foreach (int leaving in LeavingTree(cardId))
        {
            DiscardCard(leaving);
        }
    }

    private List<int> LeavingTree(int cardId)
    {
        var leaving = new List<int> { cardId };
        var pending = new Stack<Card>(ChildrenOf(cardId).Reverse());
        var seen = new HashSet<int> { cardId };
        while (pending.TryPop(out var hosted))
        {
            RefuseInvalidAttachment(hosted, seen, cardId);
            leaving.Add(hosted.ObjectId);
            foreach (var child in ChildrenOf(hosted.ObjectId).Reverse())
            {
                pending.Push(child);
            }
        }
        return leaving;
    }

    private IEnumerable<Card> ChildrenOf(int host) => cast.World.Areas
        .Where(area => area.Host == host)
        .SelectMany(area => area.Cards);

    private void RefuseInvalidAttachment(
        Card hosted, HashSet<int> seen, int root)
    {
        if (!seen.Add(hosted.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"attachment {hosted.ObjectId} forms a hosting cycle");
        }
        if (StateFields.Modified(
                cast.World, hosted, "permanent",
                cast.World.Facts, cast.World.Players) <= 0) return;
        // Match Discard.Attachments' complete-tree preflight so eligibility
        // refuses before an action cost can mutate.
        throw new RulesNotImplementedException(
            $"permanent attachment {hosted.ObjectId} lost host {root}, "
            + "and rr:permanent.5 is not implemented");
    }

    private void DiscardCard(int cardId)
    {
        discarded.Add(cardId);
        if (Statuses.Count(
                cast.World, cast.World.Cards[cardId], Statuses.Tough) > 0)
        {
            tough[cardId] = 0;
        }
        else tough.Remove(cardId);
        TraceStatusesLeave(cardId, cast, statusCounts, statusChanges);
        engagement.Remove(cardId);
    }
}
