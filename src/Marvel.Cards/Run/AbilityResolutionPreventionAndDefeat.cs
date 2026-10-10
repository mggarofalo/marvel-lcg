using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionPreventionAndDefeat
{
    internal static void Use(this AbilityResolutionExecution execution,
        World world, Card card, CompiledCardAbility ability, Occurrence? occurrence = null)
        => AbilityUseRecording.Record(world, execution.program, card, ability, occurrence);

    /// <inheritdoc/>
    internal static long WouldBeDealt(this AbilityResolutionExecution execution,
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return amount;
        }

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);

        long left = amount;
        foreach (var (card, ability) in execution.Waiting(world, occurrence))
        {
            // **Forced only.** `rr:ability.11` makes everything optional unless
            // prefaced by "Forced", and an optional interrupt is a question --
            // which needs a window, which dealing damage has not got. A card
            // that would ask here is refused by name rather than resolved
            // without asking.
            if (ability.Trigger.Timing != AbilityType.ForcedInterrupt)
            {
                // Optional interrupts are offered by the agenda before attack
                // damage is applied. A direct damage call has no window, so it
                // cannot trigger one and must not resolve it on the player's
                // behalf.
                continue;
            }

            var cast = new AbilityResolutionState(world, card, occurrence, target.Owner, events)
            {
                Incoming = left,
                Tier = ability.Trigger.Timing,
            };

            execution.TrackResolution(cast, ability);
            execution.Run(ability, cast);
            cast.CompleteResolution();

            // An ability that touched the damage says so; one that did nothing
            // to it leaves it alone. `rr:damage.step.1` holds abilities that
            // *may* replace the damage, not ones that must.
            left = cast.Remaining < 0 ? left : cast.Remaining;
            if (left <= 0)
            {
                // `rr:replacement-effect.1` -- "when an effect is replaced, it
                // is no longer considered imminent and no further interrupts or
                // responses to that effect can be triggered."
                return 0;
            }
        }

        return left;
    }

    /// <inheritdoc/>
    internal static long WouldTake(this AbilityResolutionExecution execution,
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);
        if (prevention is null || !world.Effects.Use(prevention))
        {
            return amount;
        }

        long prevented = prevention.Amount <= 0 ? amount : prevention.Amount;
        return Math.Max(0, amount - prevented);
    }

    /// <inheritdoc/>
    internal static void DamagePreventedByTough(this AbilityResolutionExecution execution,
        World world, Card target, Card source, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId).ToList();
        foreach (var effect in prevention)
        {
            world.Effects.Use(effect);
        }
    }

    /// <inheritdoc/>
    internal static void WouldBeDefeated(this AbilityResolutionExecution execution, World world, Card target, List<GameEvent> events)
    {
        _ = execution.WouldBeDefeated(
            world, target, target, Steps.CardWouldBeDefeated,
            Steps.CardWouldBeDefeated, -1, events);
    }

    /// <inheritdoc/>
    internal static bool WouldBeDefeated(this AbilityResolutionExecution execution,
        World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        var spent = world.Agenda.Occurrence;

        while (AbilityWindow.Tiers(
            execution.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence) is { Count: > 0 } tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tiers[0]);
            if (mandatory.Count == 0)
            {
                execution.SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, optional,
                    recordDefeatOn);
                return false;
            }

            if (mandatory.Count > 1)
            {
                execution.SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, mandatory,
                    recordDefeatOn);
                return false;
            }

            occurrence.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            spent?.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            events.AddRange(execution.Resolve(world, occurrence, mandatory[0], [], []));

            // `rr:would.1`: once the interrupt changes the imminent defeat,
            // no later interrupt to that original condition may be used.
            if (DamagePlacement.Health(world, world.Facts, target) - target.Damage > 0)
            {
                return true;
            }
        }

        return true;
    }

    internal static void SuspendWouldBeDefeated(this AbilityResolutionExecution execution,
        World world, Card target, Card source, string trigger, string verb, int by,
        Occurrence occurrence, IReadOnlyList<PendingAbility> pending,
        Occurrence? recordDefeatOn)
    {
        var step = new PhaseStep(
            Steps.ChooseWouldBeDefeated,
            world.Agenda.Current?.Round ?? 0,
            6,
            Subject: target.ObjectId,
            Seat: target.Owner >= 0 ? target.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureOwnerOccurrence: recordDefeatOn,
            ProcedureSource: source.ObjectId,
            ProcedureTrigger: trigger,
            ProcedureVerb: verb,
            ProcedureBy: by);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    internal static List<(Card Card, CompiledCardAbility Ability)> Waiting(this AbilityResolutionExecution execution, World world, Occurrence what) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .SelectMany(card => execution.On(card)
                .Where(ability => execution.Answers(world, ability, card, what))
                .Select(ability => (Card: card, Ability: ability)))
            .ToList(),
    ];

    /// <summary>Whether one ability answers this occurrence at all.</summary>
    internal static bool Answers(this AbilityResolutionExecution execution,
        World world, CompiledCardAbility ability, Card card, Occurrence what)
    {
        int? restricted = execution.RestrictedPlayer(world, ability, card);
        return ability.Trigger.Event is { } condition
            && what.Conditions.Contains(condition, StringComparer.Ordinal)
            && execution.Subject(world, ability.Trigger.Subject, card, what, restricted)
            && (ability.Trigger.Actor != AbilityRoles.You || what.ActorFacts?.Kind is CardKind.Hero or CardKind.AlterEgo)
            && execution.Role(world, ability.Trigger.Actor, card, what.ActorFacts, restricted)
            && execution.Role(world, ability.Trigger.Target, card, what.TargetFacts, restricted)
            && execution.Player(world, ability.Trigger.Player, card, what, restricted);
    }

}
