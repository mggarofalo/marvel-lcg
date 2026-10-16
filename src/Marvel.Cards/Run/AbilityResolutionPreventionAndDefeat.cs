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

    internal static IReadOnlyList<GameEvent> WouldBeDefeated(this AbilityResolutionExecution execution,
        World world, Card target, DefeatCause cause, Occurrence occurrence)
    {
        var pending = execution.Waiting(world, occurrence, WindowKind.Interrupt);
        if (AbilityWindow.Tiers(pending, WindowKind.Interrupt, occurrence).Count == 0) return [];
        var continuation = new PhaseStep(
            new DefeatAbilityOperation(DefeatAbilityKind.ResumeWouldBeDefeated, cause,
                new DefeatWindow(occurrence)), world.Agenda.Current?.Round ?? 0, 6,
            Subject: target.ObjectId, Seat: target.Owner >= 0 ? target.Owner : world.FirstPlayer, Plan: true);
        if (world.Agenda.Current is { } owner && world.Agenda.Occurrence is { } containing)
            world.Agenda.ContinueBeforeOwner(containing, owner, continuation);
        else world.Agenda.Add(continuation);
        return [];
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
