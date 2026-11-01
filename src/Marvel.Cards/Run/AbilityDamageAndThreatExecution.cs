using static Marvel.Cards.Run.AbilityDamageAndThreatQueries;
using static Marvel.Cards.Run.AbilityThreatExecution;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Executes the rule procedures that change damage or threat. The runner owns
// structural continuation persistence; this owner returns only the small set
// of domain facts that must cross that boundary.
internal static class AbilityDamageAndThreatExecution
{
    internal static List<Card> Assignable(
        AbilityCardSelection selection, AbilityDamageAndThreatContext context) =>
        Assignable(Every(selection, context), context);

    internal static IReadOnlyList<Card> DamageTargets(
        AbilityCardSelection selection, AbilityDamageAndThreatContext context) =>
        DamageTargets(Every(selection, context), context);

    internal static long Room(Card card, AbilityDamageAndThreatContext context) =>
        Room(context, card);

    internal static AbilityDamageAndThreatResult ResolveAssigned(
        AbilityEffect syntax, IReadOnlyDictionary<int, long> assigned,
        AbilityDamageAndThreatContext context)
    {
        var state = new AbilityDamageAndThreatState();
        ResolveAssigned(syntax, context, state, assigned);
        return state.ToResult();
    }

    internal static AbilityDamageAndThreatResult DealDamage(
        AbilityEffect.Damage damage, AbilityEffect syntax,
        AbilityDamageAndThreatContext context, long multiplier)
    {
        var state = new AbilityDamageAndThreatState();
        DealDamage(damage, syntax, context, state, multiplier);
        return state.ToResult();
    }

    internal static AbilityDamageAndThreatResult RemoveThreat(
        AbilityEffect.RemoveThreat removal, AbilityDamageAndThreatContext context,
        long multiplier)
    {
        var state = new AbilityDamageAndThreatState();
        AbilityEffectBoundary.Begin(context, state);
        state.Outcome = ExecuteRemoveThreat(removal, context, state, multiplier);
        return state.ToResult();
    }

    internal static AbilityDamageAndThreatResult Run(
        AbilityEffect instruction, AbilityEffect syntax, AbilityDamageAndThreatContext context)
    {
        var state = new AbilityDamageAndThreatState();
        if (RunDamageInstruction(instruction, syntax, context, state))
            return state.ToResult();
        if (RunThreatInstruction(instruction, syntax, context, state))
            return state.ToResult();
        return AbilityDamageAndThreatResult.NotHandled;
    }

    private static bool RunDamageInstruction(
        AbilityEffect instruction, AbilityEffect syntax,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        switch (instruction)
        {
            case AbilityEffect.Damage { Maximum: not null } when context.SelectedDamageRecipients is null:
                state.Suspension = AbilityDamageAndThreatSuspension.Choice;
                return true;
            case AbilityEffect.Damage damage:
                DealDamage(damage, syntax, context, state);
                return true;
            case AbilityEffect.AttackDamage damage:
                AbilityAttackDamageExecution.Run(damage, context, state);
                return true;
            case AbilityEffect.MoveDamage { Attack: false } movement:
                MoveDamage(movement, syntax, context, state);
                return true;
            case AbilityEffect.MoveDamage movement:
                MoveAttackDamage(movement, syntax, context, state);
                return true;
            case AbilityEffect.IndirectDamage damage:
                Indirect(damage, syntax, context, state);
                return true;
            case AbilityEffect.Heal heal:
                Heal(heal, context, state); return true;
            case AbilityEffect.SetHitPoints assignment:
                state.Outcome = AbilityHitPointAssignment.Execute(assignment, context);
                return true;
            case AbilityEffect.PreventDamage prevention:
                PreventDamage(prevention, context); state.ResolveEffect = true; return true;
            default: return false;
        }
    }

    private static bool RunThreatInstruction(
        AbilityEffect instruction, AbilityEffect syntax,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        switch (instruction)
        {
            case AbilityEffect.PlaceThreat threat:
                PlaceThreat(threat, context, state); return true;
            case AbilityEffect.RemoveThreat removal:
                AbilityEffectBoundary.Begin(context, state);
                state.Outcome = ExecuteRemoveThreat(removal, context, state); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.SoakDamage } soak:
                Soak(soak.Selection, context, state); return true;
            case AbilityEnemyDamageEffect.IncreaseExcessDamage increase:
                state.Remaining = AbilityExcessDamageIncrease.Amount(increase, context); return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReplaceThreatWithDamage } replacement:
                ReplaceThreatWithDamage(replacement.Selection, syntax, context, state); return true;
            case AbilityEffect.PreventThreat prevention:
                PreventThreat(prevention, context); state.ResolveEffect = true; return true;
            default: return false;
        }
    }

    private static void Heal(AbilityEffect.Heal heal, AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        state.Healed = Find(heal.Card, context) is { } target
            ? DamageRecovery.Heal(context.World, context.World.Facts, target, Amount(heal.Amount, context),
                context.Trigger, "Heal", context.Events)
            : 0;
        state.Outcome = AbilityCompletion.Amount(state.Healed.Value, Amount(heal.Amount, context));
    }

    private static void Indirect(AbilityEffect.IndirectDamage damage, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        long amount = AbilityEventModifiers.DamageAmount(
            context.World, context.Source, Amount(damage.Amount, context));
        var eligible = Assignable(Every(damage.Among, context), context);
        if (amount <= 0 || eligible.Count == 0) return;
        if (eligible.Count == 1)
        {
            Assign(node, context, state, [eligible[0]], amount);
            return;
        }
        state.Suspension = AbilityDamageAndThreatSuspension.Choice;
    }

    private static List<Card> Assignable(IReadOnlyList<Card> among, AbilityDamageAndThreatContext context) =>
    [.. among.Where(card => Room(context, card) > 0
        && AbilityProgramQueries.CanTakeDamage(
            context.World, context.Program, card, context.Source))];

    private static IReadOnlyList<Card> DamageTargets(IReadOnlyList<Card> targets, AbilityDamageAndThreatContext context) =>
        [.. targets.Where(target => AbilityProgramQueries.CanTakeDamage(
            context.World, context.Program, target, context.Source))];

    private static long Room(AbilityDamageAndThreatContext context, Card card) =>
        DamagePlacement.Health(context.World, context.World.Facts, card) - card.Damage;

    private static void Assign(AbilityEffect node, AbilityDamageAndThreatContext context,
        AbilityDamageAndThreatState state, IReadOnlyList<Card> among, long amount)
    {
        var assigned = new Dictionary<int, long>();
        long left = amount;
        foreach (var card in among)
        {
            if (left <= 0) break;
            long take = Math.Min(Room(context, card), left);
            if (take <= 0) continue;
            assigned[card.ObjectId] = take;
            left -= take;
        }
        ResolveAssigned(node, context, state, assigned);
    }

    private static void ResolveAssigned(AbilityEffect node, AbilityDamageAndThreatContext context,
        AbilityDamageAndThreatState state, IReadOnlyDictionary<int, long> assigned)
    {
        AbilityEffectBoundary.Begin(context, state);
        bool suspended = false;
        var occurrence = context.World.Agenda.Occurrence;
        foreach (var (card, damage) in assigned.OrderBy(each => each.Key))
        {
            var request = new DamageRequest(context.Source, context.World.Cards[card], damage,
                context.Trigger, "Indirect_Damage", AbilityEffectAttribution.IdentityPlayer(context))
                { RecordDefeatOn = occurrence, ActivationId = context.DamageActivationId };
            suspended |= DamagePlacement.Resolve(context.World, context.World.Facts, request,
                context.Events).Outcome == Damage.Outcome.Suspended;
        }
        if (suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void DealDamage(AbilityEffect.Damage damage, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state, long multiplier = 1)
    {
        AbilityEffectBoundary.Begin(context, state);
        long amount = ModifiedAbilityDamage(AbilityAmounts.SaturatingMultiply(
            Amount(damage.Amount, context), multiplier), context);
        state.Damage = new(context.Occurrence);
        var requests = (context.SelectedDamageRecipients ?? Every(damage.Cards, context)).Select(target =>
            new DamageRequest(context.Source, target, amount, context.Trigger,
                damage.AttackVerb ? "Attack" : "Deal_Damage", AbilityEffectAttribution.IdentityPlayer(context))
            {
                RecordDefeatOn = context.World.Agenda.Occurrence,
                ActivationId = context.DamageActivationId,
                IgnoresTough = damage.IgnoresTough,
            }).ToArray();
        if (requests.Length > 1)
            state.Damage.Add(SimultaneousDirectDamage.Resolve(context.World, context.World.Facts, requests, context.Events),
                amount, context.Power == BasicPowers.AttackVerb);
        else if (requests.Length == 1)
            state.Damage.Add(DamagePlacement.Resolve(context.World, context.World.Facts, requests[0], context.Events),
                amount, context.Power == BasicPowers.AttackVerb);
        if (!state.Damage.IsComplete) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static long ModifiedAbilityDamage(long amount, AbilityDamageAndThreatContext context)
    {
        amount = AbilityAmounts.SaturatingSum(amount, [AbilityEventModifiers.Amount(context.World, context.Source, "eventDamage")]);
        return context.Power == BasicPowers.AttackVerb
            ? AbilityAmounts.SaturatingSum(amount, [AbilityEventModifiers.Amount(context.World, context.Source, "attackDamage")])
            : amount;
    }

    private static void MoveDamage(AbilityEffect.MoveDamage movement, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        AbilityEffectBoundary.Begin(context, state);
        var from = Find(movement.From, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the enemy damage moves to");
        long amount = Math.Min(from.Damage, Amount(movement.Amount, context));
        if (amount <= 0 || !AbilityProgramQueries.CanTakeDamage(
                context.World, context.Program, to, context.Source)) return;
        DamageRecovery.Heal(context.World, context.World.Facts, from, amount, context.Trigger, "Move_Damage", context.Events);
        var request = new DamageRequest(context.Source, to, amount,
            context.Trigger, "Attack", AbilityEffectAttribution.IdentityPlayer(context))
            { RecordDefeatOn = context.World.Agenda.Occurrence };
        if (DamagePlacement.Resolve(context.World, context.World.Facts, request,
            context.Events).Outcome == Damage.Outcome.Suspended)
            state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void MoveAttackDamage(AbilityEffect.MoveDamage movement, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        AbilityEffectBoundary.Begin(context, state);
        var from = Find(movement.From, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the enemy damage moves to");
        state.Attacked.Add(to);
        long amount = Math.Min(from.Damage, Amount(movement.Amount, context));
        if (amount <= 0 || !AbilityProgramQueries.CanTakeDamage(
                context.World, context.Program, to, context.Source)) return;
        DamageRecovery.Heal(context.World, context.World.Facts, from, amount, context.Trigger, "Move_Damage", context.Events);
        var damaged = DamageAttacks.Attack(context.World, context.World.Facts,
            context.PowerActor ?? context.AbilityActor ?? context.World.Seats[Resolver(context)].IdentityCard,
            context.Source, to, amount, context.Trigger, BasicPowers.AttackVerb, context.Events, retaliate: false);
        (state.Damage ??= new(context.Occurrence)).Add(damaged, amount);
        if (!damaged.IsComplete) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void Soak(AbilityCardSelection selection, AbilityDamageAndThreatContext context,
        AbilityDamageAndThreatState state)
    {
        var onto = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would soak damage onto a card that is not there");
        long before = onto.Damage;
        onto.TakeDamage(context.Incoming);
        context.Events.Add(new FieldSet(onto.ObjectId, "k_damage", before, onto.Damage)
        { Trigger = context.Trigger, Verb = "Place_Damage" });
        state.Remaining = 0;
    }

    private static void PreventDamage(AbilityEffect.PreventDamage prevention, AbilityDamageAndThreatContext context)
    {
        long amount = Amount(prevention.Amount, context);
        if (prevention.Payment is { } payment && !SpendCounters(payment, ref amount, context)) return;
        if (context.Occurrence.DirectDamage is { } direct)
        {
            direct.Prevent(context.World, amount);
            return;
        }
        int target = context.Occurrence.Target >= 0 ? context.Occurrence.Target : context.Occurrence.Subject;
        context.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "preventDamage", Amount: amount, Card: context.Source.ObjectId,
            Affects: target, Lasts: new Duration(Uses: 1)));
    }

    // Ruling a1a372ea9b59ba4e: the ability does as much as it can -- three counters
    // against five damage remove three and prevent three.
    private static bool SpendCounters(AbilityCounterPayment payment, ref long amount, AbilityDamageAndThreatContext context)
    {
        var holder = Find(payment.Holder, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would prevent damage with counters on a card that is not there");
        amount = Math.Min(amount, DamageWindowEligibility.RemainingDamage(context.World, context.Occurrence));
        // Equivalent mutant: the trigger needs remaining damage and a counter, so a
        // resolved payment is positive; the guard keeps an empty payment from registering.
        if (amount <= 0) return false;
        AbilityCardOperations.RemoveCounters(context.World, context.World.CounterPools, holder, payment.Counter,
            amount, context.Trigger, context.Events);
        return true;
    }
}
