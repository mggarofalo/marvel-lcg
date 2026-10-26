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
    private Dictionary<int, int>? encounterStages;
    private int[]? encounterOrder;
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
        encounterStages = state.EncounterStages;
        encounterOrder = state.EncounterOrder;
        peak = state.PeakTargetPressure;
    }

    internal DamageTraceState Apply(IReadOnlyList<DamageTransfer> trace)
    {
        foreach (var transfer in trace)
        {
            if (finished) break;
            Apply(transfer);
        }
        return Snapshot();
    }

    private DamageTraceState Snapshot() => new DamageTraceState(
            damage, peak, original.Players, tough, statusChanges,
            statusCounts, health, discarded, threat, traits, modifiers,
            engagement, formsMayChange, firstPlayer, currentVillain,
            villainStagesDrawn, finished)
        { EncounterStages = encounterStages, EncounterOrder = encounterOrder };

    private void Apply(DamageTransfer transfer)
    {
        int from = ResolveCard(transfer.FromVillain, transfer.From, transfer.FromSingular);
        int to = ResolveCard(transfer.ToVillain, transfer.To, transfer.ToSingular);
        if (from == int.MinValue || to == int.MinValue) return;
        if (ApplyStructural(transfer, to)) return;
        if (ApplyGrant(transfer, to)) return;
        ApplyDamage(transfer, from, to);
    }

    private int ResolveCard(AbilityCardSelection? selector, int fallback, AbilityCardSelection? singular)
    {
        if (selector is null && singular is null) return fallback;
        var trace = new AbilitySelectorTraceContext(currentVillain, cast, discarded, traits, modifiers, engagement)
            { EncounterStages = encounterStages, EncounterOrder = encounterOrder,
                SchemeThreat = threat, StatusCounts = statusCounts };
        return singular is not null
            ? AbilitySingularTraceSelection.Find(singular, trace)?.ObjectId ?? int.MinValue
            : TraceSelectorIncludesCard(selector!, fallback, trace) ?? int.MinValue;
    }

    private bool ApplyStructural(DamageTransfer transfer, int to)
    {
        if (transfer.ChangesForm >= 0) ChangeForm(transfer);
        else if (transfer.MovesActiveCounter)
        {
            if (AbilityActiveCounterProjection.CanMove(cast.World, currentVillain,
                cast.World.Cards[to], encounterStages, discarded)) currentVillain = to;
        }
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

    private void GrantStatus(int cardId, string status) =>
        new DamageTraceStatuses(Snapshot(), cast).GrantStatus(cardId, status);

    private void SetTough(Card card, int count) =>
        new DamageTraceStatuses(Snapshot(), cast).SetTough(card, count);

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
            ? AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
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
            ? AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
                cast, to, amount, damage, discarded, currentVillain)
            : amount;
        if (to >= 0) AssignDamage(to, landed);
        ObserveTarget();
    }

    private bool CanLand(DamageTransfer transfer, int to) =>
        to < 0
        || !transfer.DealsDamage
        || AbilityDamageProtectionTrace.CanTakeDamageInTrace(cast, cast.World.Cards[to], discarded);

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
        if ((encounterStages?.ContainsValue(cardId) ?? cardId == currentVillain)
            && Current(cardId) >= tracedHealth)
        {
            ResolveVillainDefeat(card);
            return;
        }
        ResolveInPlayDefeat(card, tracedHealth);
        ResolveIdentityDefeat(card, tracedHealth);
    }

    private void ResolveVillainDefeat(Card card)
    {
        var advanced = new DamageTraceVillains(Snapshot(), cast).Resolve(card);
        currentVillain = advanced.CurrentVillain;
        villainStagesDrawn = advanced.VillainStagesDrawn;
        finished = advanced.Finished;
        encounterStages = advanced.EncounterStages;
        encounterOrder = advanced.EncounterOrder;
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

    private void ResolveIdentityDefeat(Card card, long tracedHealth) =>
        firstPlayer = new DamageTraceDepartures(Snapshot(), cast).ResolveIdentityDefeat(card, tracedHealth);

    private void LeavePlay(int cardId) => new DamageTraceDepartures(Snapshot(), cast).LeavePlay(cardId);
}
