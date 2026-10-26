using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Read-only trigger-window eligibility shared by live window discovery and
// initiation reachability. It evaluates one occurrence lazily and owns no
// event sink, continuation frame, or runner callback.
internal static class AbilityWindowAdmission
{
    internal sealed record Candidate(
        Card Card, CompiledCardAbility Ability, int Controller, int Ordinal)
    {
        internal Defeated? TriggeringDefeat { get; init; }
        internal Attacked? TriggeringAttack { get; init; }
        internal Occurrence? TriggeringThreat { get; init; }
    }

    internal static ImmutableHashSet<int> WaitingCards(
        AbilityProgram program, World world, Occurrence occurrence,
        WindowKind window, IResourceCardAbilities resourceAbilities) =>
        Waiting(program, world, occurrence, window, resourceAbilities)
            .Select(candidate => candidate.Card.ObjectId)
            .ToImmutableHashSet();

    internal static ImmutableArray<Candidate> Waiting(
        AbilityProgram program, World world, Occurrence occurrence,
        WindowKind window, IResourceCardAbilities resourceAbilities)
    {
        var waiting = ImmutableArray.CreateBuilder<Candidate>();
        foreach (var card in world.Cards)
        {
            waiting.AddRange(CandidatesFor(
                program, world, card, occurrence, window, resourceAbilities));
        }
        return waiting.ToImmutable();
    }

    private static IEnumerable<Candidate> CandidatesFor(
        AbilityProgram program, World world, Card card, Occurrence occurrence,
        WindowKind window, IResourceCardAbilities resourceAbilities)
    {
        if (!CanHostWindowAbility(world, card)) yield break;
        var written = program.On(card.FaceId);
        for (int index = 0; index < written.Length; index++)
        {
            var ability = written[index];
            foreach (int controller in Players(program, world, ability, card, occurrence))
            {
                int ordinal = written.Take(index).Count(candidate =>
                    candidate.Trigger.Timing == ability.Trigger.Timing);
                var candidate = new Candidate(card, ability, controller, ordinal);
                foreach (var instance in AbilityTriggerInstances.For(candidate, occurrence))
                    if (IsWaitingCandidate(program, world, instance, occurrence, window, resourceAbilities))
                        yield return instance;
            }
        }
    }

    private static bool CanHostWindowAbility(World world, Card card)
    {
        if (EffectiveCards.HasProfile(card) || PrintedText.IsBlanked(world, card)) return false;
        bool playableEvent = CardPlayOrigin.CanPlayEvent(world, card, card.Owner);
        return DeckTypes.IsInPlay(card.Area.Type) || playableEvent;
    }

    private static bool IsWaitingCandidate(
        AbilityProgram program, World world, Candidate candidate,
        Occurrence occurrence, WindowKind window, IResourceCardAbilities resourceAbilities)
    {
        var (card, ability, controller, _) = candidate;
        // rr:when-completed-abilities.1: "When this scheme is completed..."
        if (!TriggerIsLive(world, ability, card, occurrence)) return false;
        occurrence = candidate.TriggeringThreat ?? occurrence;
        if (!Answers(program, world, candidate, occurrence, window,
                ability.AnyPlayer ? controller : null)
            || !InForm(world, controller, ability.Trigger.Form, card)) return false;
        var context = Context(program, world, card, occurrence, controller, ability.Cost, resourceAbilities);
        context = context.WithQuery(context.Query with { TriggeringDefeat = candidate.TriggeringDefeat, TriggeringAttack = candidate.TriggeringAttack });
        return WhenHolds(ability, context)
            && ControllerCanInitiate(controller, ability, context)
            && AbilityPaymentRules.Payable(world, card, controller, ability.Cost, program, resourceAbilities)
            && AbilityPaymentRules.EventPayable(world, card, controller, ability, resourceAbilities)
            && AbilityAvailability.Available(world, card, ability,
                AbilityAvailability.IndexOf(program, card, ability), occurrence);
    }

    private static bool ControllerCanInitiate(int controller, CompiledCardAbility ability,
        AbilityAdmissionContext context) => controller < 0 || CanInitiate(ability, context);

    private static bool TriggerMatchesCandidate(CompiledCardAbility ability, Card card, Occurrence occurrence) =>
        TriggerMatches(ability, occurrence) && (ability.Trigger.Timing != AbilityType.WhenCompleted
            || occurrence.Is(MainSchemeCompletionOperation.Completed) && occurrence.Subject == card.ObjectId);

    private static bool TriggerIsLive(World world, CompiledCardAbility ability, Card card, Occurrence occurrence) =>
        TriggerMatchesCandidate(ability, card, occurrence)
        && (ability.Trigger.Damage != DamageTriggerCondition.Taken
            || DamageWindowEligibility.HasRemainingDamage(world, occurrence));

    private static AbilityAdmissionContext Context(
        AbilityProgram program, World world, Card card, Occurrence occurrence,
        int player, AbilityCost? cost, IResourceCardAbilities resourceAbilities)
    {
        var query = new AbilityQueryContext(
            world, card, occurrence, player, card.Incarnation,
            null, null, null, []);
        var expressions = new AbilityExpressionContext(
            query, ImmutableDictionary<string, long>.Empty, [], string.Empty,
            -1, false, null);
        return new AbilityAdmissionContext(
            program, resourceAbilities, expressions,
            new AbilityReachabilityContext
            {
                PaymentMayMutate = cost is not null
                    || EffectiveCards.Kind(card, world.Facts) == CardKind.Event,
                PaymentCost = cost,
            },
            Power: null);
    }

    private static bool CanInitiate(
        CompiledCardAbility ability, AbilityAdmissionContext context)
    {
        if (!AbilityInitiation.LabelsCanInitiate(ability, context))
        {
            return false;
        }
        if (ability.Labels.Length > 0
            && LabeledAbilities.WouldBeCancelled(
                context.World, context.World.Facts,
                AbilityCardQueries.Resolver(context.Query),
                context.Source, ability.Labels))
        {
            return true;
        }
        return ability.Cost is AbilityCost.SpendEnergy
            ? AbilityPaymentReceiptAdmission.Admit(ability, context, null) is not null
            : AbilityEntryAdmission.Admit(ability.Effect, context).IsAdmissible;
    }

    private static bool WhenHolds(
        CompiledCardAbility ability, AbilityAdmissionContext context) =>
        ability.When is not { } condition || context.Evaluator().Test(condition);

    private static IEnumerable<int> Players(
        AbilityProgram program, World world, CompiledCardAbility ability,
        Card card, Occurrence occurrence)
    {
        if (!ability.AnyPlayer)
        {
            return [Controller(program, world, ability, card, occurrence)];
        }
        if (ability.Trigger.Player == AbilityPlayers.TriggerPlayer
            && occurrence.Player >= 0)
        {
            return [occurrence.Player];
        }
        return world.PlayerOrder;
    }

    private static int Controller(
        AbilityProgram program, World world, CompiledCardAbility ability,
        Card card, Occurrence occurrence) =>
        RestrictedPlayer(program, world, ability, card) is { } restricted
            ? restricted
            : ability.Trigger.Player is not null
                ? occurrence.Player
                : ability.Trigger.Actor == AbilityRoles.You
                    ? occurrence.ActorFacts?.Controller
                        ?? AbilityCardQueries.ControllerOf(world, card)
                    : AbilityCardQueries.ControllerOf(world, card);

    private static bool Answers(
        AbilityProgram program, World world, Candidate candidate,
        Occurrence occurrence, WindowKind window,
        int? initiatingPlayer)
    {
        var (card, ability, _, _) = candidate;
        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };
        int? restricted = initiatingPlayer
            ?? RestrictedPlayer(program, world, ability, card);
        return belongs && RolesMatch(world, candidate, occurrence, restricted);
    }

    private static bool TriggerMatches(CompiledCardAbility ability, Occurrence occurrence) =>
        ability.Trigger.Event is { } condition
        && occurrence.Is(condition)
        && (ability.Trigger.Also is not { } also || occurrence.Is(also));

    private static bool RolesMatch(
        World world, Candidate candidate, Occurrence occurrence, int? restricted)
    {
        var (card, ability, _, _) = candidate;
        return Subject(world, ability.Trigger.Subject, card, occurrence, restricted, candidate.TriggeringDefeat)
            && (ability.Trigger.Actor != AbilityRoles.You || occurrence.ActorFacts?.Kind is CardKind.Hero or CardKind.AlterEgo)
            && Role(world, ability.Trigger.Actor, card, occurrence.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, candidate.TriggeringAttack?.Facts ?? occurrence.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, occurrence, restricted);
    }

    private static int? RestrictedPlayer(
        AbilityProgram program, World world, CompiledCardAbility ability, Card card)
    {
        if (ability.AnyPlayer)
        {
            return null;
        }
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Obligation
            && card.Area.PlayArea.IsPlayers)
        {
            return card.Area.PlayArea.Player;
        }
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Attachment
            && card.Area.Host >= 0
            && card.Area.Host < world.Cards.Count
            && AbilityPlayerBindingAnalysis.UsesYouOrYour(program, ability, card))
        {
            int controller = AbilityCardQueries.ControllerOf(
                world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }
        return null;
    }

    private static bool Subject(
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted, Defeated? defeat) => subject switch
        {
            null => true,
            AbilitySubjects.This => (defeat?.Card ?? occurrence.Subject) == card.ObjectId,
            AbilitySubjects.AttachedTo =>
                card.Area.Host >= 0 && (defeat?.Card ?? occurrence.Subject) == card.Area.Host,
            AbilitySubjects.You => occurrence.Player >= 0
                && occurrence.Player
                    == (restricted ?? AbilityCardQueries.ControllerOf(world, card)),
            AbilitySubjects.Game => true,
            _ => throw new AbilityException(
                $"'{subject}' is not a subject anything matches"),
        };

    private static bool Role(
        World world, string? match, Card card, OccurrenceCard? role,
        int? restricted) => match switch
        {
            null => true,
            _ when role is null => false,
            AbilityRoles.This => role.Card == card.ObjectId,
            AbilityRoles.AttachedTo =>
                card.Area.Host >= 0 && role.Card == card.Area.Host,
            AbilityRoles.You => role.Controller >= 0
                && (restricted is { } player
                    ? role.Controller == player
                    : card.Owner == World.Scenario
                        || role.Controller == AbilityCardQueries.ControllerOf(world, card)),
            AbilityRoles.Villain => role.IsVillain,
            AbilityRoles.Minion => role.IsMinion,
            AbilityRoles.Hero => role.IsHero,
            AbilityRoles.Ally => role.IsAlly,
            AbilityRoles.Friendly => role.IsFriendly,
            AbilityRoles.Enemy => role.IsEnemy,
            _ => throw new AbilityException(
                $"'{match}' is not an occurrence role matcher"),
        };

    private static bool Player(
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted) => match switch
        {
            null or AbilityPlayers.TriggerPlayer => true,
            AbilityPlayers.You => occurrence.Player >= 0
                && occurrence.Player
                    == (restricted ?? AbilityCardQueries.ControllerOf(world, card)),
            _ => throw new AbilityException(
                $"'{match}' is not an occurrence player matcher"),
        };

    private static bool InForm(
        World world, int player, string? form, Card card)
    {
        if (form is null)
        {
            return true;
        }
        return player >= 0
            ? Forms.In(world, world.Seats[player], world.Facts, form)
            : throw new RulesNotImplementedException(
                $"'{card.FaceId}' requires '{form}' form and is offered to every player, "
                + "so there is no identity whose form to read");
    }

}
