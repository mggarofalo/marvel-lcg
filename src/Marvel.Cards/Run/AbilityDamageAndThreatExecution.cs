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
        ExecuteRemoveThreat(removal, context, multiplier);
        return new AbilityDamageAndThreatState().ToResult();
    }

    internal static AbilityDamageAndThreatResult Run(
        AbilityEffect instruction, AbilityEffect syntax, AbilityDamageAndThreatContext context)
    {
        var state = new AbilityDamageAndThreatState();
        switch (instruction)
        {
            case AbilityEffect.Damage damage:
                DealDamage(damage, syntax, context, state);
                break;
            case AbilityEffect.AttackDamage damage:
                DealAttackDamage(damage, syntax, context, state);
                break;
            case AbilityEffect.MoveDamage { Attack: false } movement:
                MoveDamage(movement, syntax, context, state);
                break;
            case AbilityEffect.MoveDamage movement:
                MoveAttackDamage(movement, syntax, context, state);
                break;
            case AbilityEffect.IndirectDamage damage:
                Indirect(damage, syntax, context, state);
                break;
            case AbilityEffect.PlaceThreat threat:
                PlaceThreat(threat, context, state);
                break;
            case AbilityEffect.RemoveThreat removal:
                ExecuteRemoveThreat(removal, context);
                break;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.SoakDamage } soak:
                Soak(soak.Selection, context, state);
                break;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReplaceThreatWithDamage } replacement:
                ReplaceThreatWithDamage(replacement.Selection, syntax, context, state);
                break;
            case AbilityEffect.Heal heal:
                Heal(heal, context, state);
                break;
            case AbilityEffect.PreventDamage prevention:
                PreventDamage(prevention, context);
                state.ResolveEffect = true;
                break;
            case AbilityEffect.PreventThreat prevention:
                PreventThreat(prevention, context);
                state.ResolveEffect = true;
                break;
            default:
                return AbilityDamageAndThreatResult.NotHandled;
        }

        return state.ToResult();
    }

    private static void Heal(AbilityEffect.Heal heal, AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        state.Healed = Find(heal.Card, context) is { } target
            ? Damage.Heal(context.World, context.World.Facts, target, Amount(heal.Amount, context),
                context.Trigger, "Heal", context.Events)
            : 0;
    }

    private static void Indirect(AbilityEffect.IndirectDamage damage, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        long amount = Amount(damage.Amount, context);
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
        && context.World.Abilities.CanTakeDamage(context.World, card, context.Source))];

    private static IReadOnlyList<Card> DamageTargets(IReadOnlyList<Card> targets, AbilityDamageAndThreatContext context) =>
        [.. targets.Where(target => context.World.Abilities.CanTakeDamage(
            context.World, target, context.Source))];

    private static long Room(AbilityDamageAndThreatContext context, Card card) =>
        Damage.Health(context.World, context.World.Facts, card) - card.Damage;

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
        bool suspended = false;
        foreach (var (card, damage) in assigned.OrderBy(each => each.Key))
        {
            suspended |= Damage.DealOutcome(context.World, context.World.Facts, context.Source,
                context.World.Cards[card], damage, context.Trigger, "Indirect_Damage", context.Events)
                == Damage.Outcome.Suspended;
        }
        if (suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void DealDamage(AbilityEffect.Damage damage, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state, long multiplier = 1)
    {
        long amount = ModifiedAbilityDamage(AbilityAmounts.SaturatingMultiply(
            Amount(damage.Amount, context), multiplier), context);
        string verb = damage.AttackVerb ? "Attack" : "Deal_Damage";
        bool suspended = false;
        foreach (var target in Every(damage.Cards, context))
        {
            long before = target.Damage;
            suspended |= Damage.DealOutcome(context.World, context.World.Facts, context.Source, target,
                amount, context.Trigger, verb, context.Events) == Damage.Outcome.Suspended;
            if (context.Power == BasicPowers.AttackVerb && target.Damage > before)
                context.Occurrence.Also(Steps.DamageDealt);
        }
        if (suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
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
        var from = Find(movement.From, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the enemy damage moves to");
        long amount = Math.Min(from.Damage, Amount(movement.Amount, context));
        if (amount <= 0 || !context.World.Abilities.CanTakeDamage(context.World, to, context.Source)) return;
        Damage.Heal(context.World, context.World.Facts, from, amount, context.Trigger, "Move_Damage", context.Events);
        if (Damage.DealOutcome(context.World, context.World.Facts, context.Source, to, amount,
            context.Trigger, "Attack", context.Events) == Damage.Outcome.Suspended)
            state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void DealAttackDamage(AbilityEffect.AttackDamage damage, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        var attacker = context.PowerActor ?? context.AbilityActor
            ?? context.World.Seats[Resolver(context)].IdentityCard;
        ContinuousEffect? temporaryOverkill = null;
        if (damage.Overkill)
        {
            temporaryOverkill = new ContinuousEffect(EffectSource.LastingEffect, Kind: Keywords.Overkill,
                Amount: 1, Card: context.Source.ObjectId, Affects: attacker.ObjectId,
                Lasts: new Duration(Uses: 1));
            context.World.Effects.Register(temporaryOverkill);
        }
        var attackModifiers = AbilityEventModifiers.Effects(context.World, context.Source, "attackDamage");
        long amount = AbilityAmounts.SaturatingSum(Amount(damage.Amount, context),
            [AbilityEventModifiers.Amount(context.World, context.Source, "eventDamage"),
             AbilityAmounts.SaturatingSum(0, attackModifiers.Select(effect => effect.Amount))]);
        bool suspended = false;
        foreach (var target in DamageTargets(Every(damage.Cards, context), context))
        {
            var damaged = Damage.Attack(context.World, context.World.Facts, attacker, context.Source, target,
                amount, context.Trigger, "Attack", context.Events, retaliate: false);
            state.Attacked.Add(target);
            if (damaged.Characters.Count > 0) context.Occurrence.Also(Steps.DamageDealt);
            suspended |= damaged.Suspended;
        }
        foreach (var modifier in context.Power == BasicPowers.AttackVerb ? [] : attackModifiers)
            context.World.Effects.Use(modifier);
        if (temporaryOverkill is not null) context.World.Effects.Use(temporaryOverkill);
        if (suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void MoveAttackDamage(AbilityEffect.MoveDamage movement, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        var from = Find(movement.From, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the enemy damage moves to");
        state.Attacked.Add(to);
        long amount = Math.Min(from.Damage, Amount(movement.Amount, context));
        if (amount <= 0 || !context.World.Abilities.CanTakeDamage(context.World, to, context.Source)) return;
        Damage.Heal(context.World, context.World.Facts, from, amount, context.Trigger, "Move_Damage", context.Events);
        var damaged = Damage.Attack(context.World, context.World.Facts,
            context.PowerActor ?? context.AbilityActor ?? context.World.Seats[Resolver(context)].IdentityCard,
            context.Source, to, amount, context.Trigger, BasicPowers.AttackVerb, context.Events, retaliate: false);
        if (damaged.Characters.Count > 0) context.Occurrence.Also(Steps.DamageDealt);
        if (damaged.Suspended) state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void PlaceThreat(AbilityEffect.PlaceThreat threat,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        var schemes = Every(threat.Schemes, context);
        if (schemes.Count == 0) return;
        long amount = Amount(threat.Amount, context);
        if (amount <= 0) return;
        if (context.HasContinuation) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' places threat before its ability has finished; the continuation must be preserved across the threat interrupt window");
        Threat.Schedule(context.World, schemes, context.Source, amount, ThreatCause.CardAbility,
            context.Trigger, context.Player, context.ResolutionAbility, context.Occurrence);
        state.Suspension = AbilityDamageAndThreatSuspension.ScheduledThreat;
    }

    private static void PreventThreat(AbilityEffect.PreventThreat prevention, AbilityDamageAndThreatContext context)
    {
        var placement = context.ImminentThreat ?? context.Occurrence.Threat
            ?? throw new RulesNotImplementedException($"'{context.Source.FaceId}' would prevent threat that is not imminent");
        placement.Prevent(Amount(prevention.Amount, context));
    }

    private static void ReplaceThreatWithDamage(AbilityCardSelection selection, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        var placement = context.Occurrence.Threat
            ?? throw new RulesNotImplementedException($"'{context.Source.FaceId}' would replace threat that is not imminent");
        long damage = placement.Remaining;
        var target = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' replaces threat with damage to a card that is not there");
        placement.Replace();
        // The replacement has applied before the nested damage procedure can
        // open a defeat window. This is the one resolution-ledger fact this
        // domain procedure must commit mid-handler.
        context.ResolveEffect();
        if (Damage.DealOutcome(context.World, context.World.Facts, context.Source, target, damage,
            context.Trigger, "Deal_Damage", context.Events) == Damage.Outcome.Suspended)
            state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static void ExecuteRemoveThreat(AbilityEffect.RemoveThreat removal,
        AbilityDamageAndThreatContext context, long multiplier = 1)
    {
        var schemes = Every(removal.Schemes, context);
        if (schemes.Count == 0) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would remove threat from a scheme that is not there");
        foreach (var scheme in schemes)
        {
            if (!removal.IgnoresCrisis && scheme.Area.Type == DeckType.MainSchemesArea
                && context.World.Facts.Kind(context.Source.FaceId) is CardKind.Event or CardKind.Ally or CardKind.Hero or CardKind.AlterEgo or CardKind.Upgrade or CardKind.Support
                && MainScheme.Crisis(context.World, context.World.Facts)) continue;
            Threat.Remove(context.World, context.World.Facts, context.World.Abilities, scheme,
                AbilityAmounts.SaturatingSum(AbilityAmounts.SaturatingMultiply(
                    Amount(removal.Amount, context), multiplier),
                    [AbilityEventModifiers.Amount(context.World, context.Source, "eventThreatRemoval")]),
                context.Trigger, "Remove_Threat", context.Events, by: Resolver(context),
                overridesCannotFrom: removal.OverridesCannotFrom is { } source
                    ? Find(source, context)?.ObjectId ?? -1 : -1);
        }
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
        int target = context.Occurrence.Target >= 0 ? context.Occurrence.Target : context.Occurrence.Subject;
        context.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "preventDamage", Amount: Amount(prevention.Amount, context), Card: context.Source.ObjectId,
            Affects: target, Lasts: new Duration(Uses: 1)));
    }

    private static long Amount(AbilityNumber number, AbilityDamageAndThreatContext context)
    {
        var selectors = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var evaluation = new AbilityExpressionEvaluation(context.Expressions, selectors);
        var result = evaluation.Result(evaluation.Amount(number));
        Publish(result, context.World);
        return result.Value;
    }

    private static Card? Find(AbilityCardSelection selection, AbilityDamageAndThreatContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var result = evaluation.Result(evaluation.Find(selection));
        Publish(result, context.World);
        return result.Value;
    }

    private static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityDamageAndThreatContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var result = evaluation.Result(evaluation.Every(selection));
        Publish(result, context.World);
        return result.Value;
    }

    private static void Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information) world.RecordInformation(observation);
    }

    private static int Resolver(AbilityDamageAndThreatContext context) =>
        AbilityCardQueries.Resolver(context.Expressions.Bindings);
}

internal sealed record AbilityDamageAndThreatContext(
    AbilityExpressionContext Expressions, string Trigger,
    List<GameEvent> Events, Card? AbilityActor, Card? PowerActor, string? Power,
    bool HasContinuation, ThreatPlacement? ImminentThreat, PendingAbility? ResolutionAbility,
    long Incoming)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal Occurrence Occurrence => Expressions.Occurrence;
    internal int Player => Expressions.Player;

    internal void ResolveEffect()
    {
        if (ResolutionAbility is { } ability) Occurrence.Resolve(ability);
    }
}

internal enum AbilityDamageAndThreatSuspension { None, Choice, Procedure, ScheduledThreat }

internal sealed class AbilityDamageAndThreatState
{
    internal long? Healed { get; set; }
    internal long? Remaining { get; set; }
    internal bool ResolveEffect { get; set; }
    internal AbilityDamageAndThreatSuspension Suspension { get; set; }
    internal List<Card> Attacked { get; } = [];
    internal AbilityDamageAndThreatResult ToResult() => new(true, Healed, Remaining, ResolveEffect, Suspension, [.. Attacked]);
}

internal sealed record AbilityDamageAndThreatResult(
    bool Handled, long? Healed, long? Remaining, bool ResolveEffect,
    AbilityDamageAndThreatSuspension Suspension, ImmutableArray<Card> Attacked)
{
    internal static AbilityDamageAndThreatResult NotHandled { get; } =
        new(false, null, null, false, AbilityDamageAndThreatSuspension.None, []);
}
