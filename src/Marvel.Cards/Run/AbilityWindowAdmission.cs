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
        Card Card, CompiledCardAbility Ability, int Controller, int Ordinal);

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
            if (FacedownDrones.Is(card))
            {
                continue;
            }
            bool eventInHand = world.Facts.Kind(card.FaceId) == CardKind.Event
                && card.Owner >= 0
                && card.Area == world.Seats[card.Owner].Hand;
            if (!DeckTypes.IsInPlay(card.Area.Type) && !eventInHand)
            {
                continue;
            }

            var written = program.On(card.FaceId);
            for (int index = 0; index < written.Length; index++)
            {
                var ability = written[index];
                IEnumerable<int> players = Players(
                    program, world, ability, card, occurrence);
                foreach (int controller in players)
                {
                    if (!Answers(
                            program, world, ability, card, occurrence, window,
                            ability.AnyPlayer ? controller : null)
                        || !InForm(world, controller, ability.Trigger.Form, card))
                    {
                        continue;
                    }

                    var context = Context(
                        program, world, card, occurrence, controller, ability.Cost,
                        resourceAbilities);
                    if (!WhenHolds(ability, context)
                        || controller >= 0 && !CanInitiate(ability, context)
                        || !AbilityPaymentRules.Payable(
                            world, card, controller, ability.Cost, program,
                            resourceAbilities)
                        || !AbilityPaymentRules.EventPayable(
                            world, card, controller, ability, resourceAbilities)
                        || !AbilityAvailability.Available(world, card, ability, index, occurrence))
                    {
                        continue;
                    }
                    int ordinal = written.Take(index).Count(candidate =>
                        candidate.Trigger.Timing == ability.Trigger.Timing);
                    waiting.Add(new Candidate(card, ability, controller, ordinal));
                }
            }
        }
        return waiting.ToImmutable();
    }

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
                    || world.Facts.Kind(card.FaceId) == CardKind.Event,
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
        return AbilityInitiation.Admit(ability.Effect, context).IsAdmissible;
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
        AbilityProgram program, World world, CompiledCardAbility ability,
        Card card, Occurrence occurrence, WindowKind window,
        int? initiatingPlayer)
    {
        if (ability.Trigger.Event is not { } condition
            || !occurrence.Is(condition)
            || ability.Trigger.Also is { } also && !occurrence.Is(also))
        {
            return false;
        }
        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };
        int? restricted = initiatingPlayer
            ?? RestrictedPlayer(program, world, ability, card);
        return belongs
            && Subject(world, ability.Trigger.Subject, card, occurrence, restricted)
            && Role(world, ability.Trigger.Actor, card, occurrence.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, occurrence.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, occurrence, restricted);
    }

    private static int? RestrictedPlayer(
        AbilityProgram program, World world, CompiledCardAbility ability, Card card)
    {
        if (ability.AnyPlayer)
        {
            return null;
        }
        if (world.Facts.Kind(card.FaceId) == CardKind.Obligation
            && card.Area.PlayArea.IsPlayers)
        {
            return card.Area.PlayArea.Player;
        }
        if (world.Facts.Kind(card.FaceId) == CardKind.Attachment
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
        int? restricted) => subject switch
    {
        null => true,
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo =>
            card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
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
