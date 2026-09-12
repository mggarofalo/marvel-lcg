using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class DefeatProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.FinalizeCharacterDefeat:
                FinalizeCharacter(world, facts, step, events);
                break;
            case Steps.FinalizeSchemeDefeat:
                FinalizeScheme(world, facts, step, events);
                break;
            case Steps.ChooseWouldBeDefeated:
                return ChooseWouldBeDefeated(world, world.WindowAbilities, step);
            case Steps.ResumeWouldBeDefeated:
                ResumeWouldBeDefeated(world, facts, world.WindowAbilities, step, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                return ChooseCardDefeatedAbility(world, world.WindowAbilities, step);
            case Steps.ResumeCardDefeatedAbility:
                ResumeCardDefeatedAbility(world, facts, world.WindowAbilities, step, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the defeat procedure has no step '{step.What}'");
        }
        return null;
    }

    internal static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseWouldBeDefeated:
                ResolveWouldBeDefeated(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                ResolveCardDefeatedAbility(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"defeat step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    internal static void ScheduleProcedureChoice(World world, PhaseStep step)
    {
        if (world.Agenda.Occurrence is { } occurrence)
        {
            world.Agenda.ThenContinuation(step, occurrence);
            return;
        }

        world.Agenda.Add(step with { Plan = true });
    }

    internal static Prompt ChooseAttachmentTarget(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var candidates = step.ProcedureCandidates ?? [];
        return new Prompt(
            world.FirstPlayer,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAttachmentTarget,
            $"{world.Seats[world.FirstPlayer].Name} chooses where {card.FaceId} attaches",
            false,
            [.. candidates.Select(id => new Affordance(
                id,
                "Attach",
                id,
                world.Cards[id].Area.PlayArea.IsPlayers
                    ? world.Cards[id].Area.PlayArea.Player
                    : -1,
                facts.Title(world.Cards[id].FaceId)))]);
    }

    internal static Prompt ChooseWouldBeDefeated(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.ProcedureAbilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 6 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardWouldBeDefeated,
            mandatory
                ? $"order abilities before card {step.Subject} is defeated"
                : $"interrupt before card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void ResolveWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        ResolveDefeatAbility(world, abilities, step, input, events, 6,
            Steps.ResumeWouldBeDefeated,
            () => FinishWouldBeDefeated(world, facts, step, events));
    }

    internal static void ResumeWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        if (DamagePlacement.Health(world, facts, target) - target.Damage > 0)
        {
            world.Effects.CompleteHealthDefeat(target);
            return;
        }

        ContinueWouldBeDefeated(world, facts, abilities, step, events);
    }

    private static void ContinueWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var procedure = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 6 has no occurrence");
        var carried = (step.ProcedureAbilities ?? []).ToList();
        if (carried.Count == 1 && AbilityTypes.IsMandatory(carried[0].Type))
        {
            var next = carried[0];
            var containing = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "damage step 6 resume has no containing occurrence");
            procedure.Trigger(WindowKind.Interrupt, next.Card);
            if (!ReferenceEquals(containing, procedure))
            {
                containing.Trigger(WindowKind.Interrupt, next.Card);
            }
            events.AddRange(abilities.Resolve(world, procedure, next, [], []));
            world.Agenda.ThenContinuation(step with
            {
                ProcedureAbilities = [],
                ProcedurePlayersPassed = [],
                Tier = next.Type,
                OccurrenceId = null,
            }, containing);
            return;
        }
        var pending = carried.Count > 0
            ? carried
            : FirstInterruptTier(abilities, world, procedure, after: step.Tier);
        if (pending.Count > 0)
        {
            ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseWouldBeDefeated,
                ProcedureAbilities = pending,
                ProcedurePlayersPassed = [],
                OccurrenceId = null,
            });
            return;
        }

        FinishWouldBeDefeated(world, facts, step, events);
    }

    private static void FinishWouldBeDefeated(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        world.Effects.CompleteHealthDefeat(target);
        if (DeckTypes.IsInPlay(target.Area.Type)
            && DamagePlacement.Health(world, facts, target) - target.Damage <= 0)
        {
            Defeat.Character(
                world, facts, target, step.ProcedureTrigger, events,
                how: step.ProcedureVerb, by: step.ProcedureBy,
                recordOn: step.ProcedureOwnerOccurrence);
        }
    }

    internal static Prompt ChooseCardDefeatedAbility(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.ProcedureAbilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 7 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardDefeated,
            mandatory
                ? $"order abilities when card {step.Subject} is defeated"
                : $"interrupt when card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void ResolveCardDefeatedAbility(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        ResolveDefeatAbility(world, abilities, step, input, events, 7,
            Steps.ResumeCardDefeatedAbility,
            () => FinalizeCardDefeat(world, facts, step, events));
    }

    private static void ResolveDefeatAbility(
        World world, IWindowAbilities abilities, PhaseStep step, Decision input,
        List<GameEvent> events, int damageStep, string resumeStep, Action finish)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException($"damage step {damageStep} has no occurrence");
        var pending = step.ProcedureAbilities ?? [];
        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        if (input.IsDecline)
        {
            ResolveDefeatDecline(world, step, pending, mandatory, finish);
            return;
        }
        ResolveChosenDefeatAbility(
            world, abilities, step, input, events, damageStep, resumeStep,
            occurrence, pending, mandatory);
    }

    private static void ResolveDefeatDecline(
        World world, PhaseStep step, IReadOnlyList<PendingAbility> pending,
        bool mandatory, Action finish)
    {
        if (mandatory)
            throw new RulesNotImplementedException(
                "a forced damage-step ability ordering cannot be declined");
        int player = ProcedurePlayer(world, step, pending, mandatory: false);
        var passed = (step.ProcedurePlayersPassed ?? []).Append(player).Distinct().ToList();
        if (HasOptionalProcedurePlayer(world, pending, passed))
            ScheduleProcedureChoice(world, step with
            { ProcedurePlayersPassed = passed, OccurrenceId = null });
        else finish();
    }

    private static void ResolveChosenDefeatAbility(
        World world, IWindowAbilities abilities, PhaseStep step, Decision input,
        List<GameEvent> events, int damageStep, string resumeStep,
        Occurrence occurrence, IReadOnlyList<PendingAbility> pending, bool mandatory)
    {
        int player = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        var chosen = offered.Select(ability =>
                (Ability: ability, Offer: abilities.Describe(world, ability)))
            .Where(pair => pair.Offer.Id == input.Affordance)
            .Select(pair => (PendingAbility?)pair.Ability).FirstOrDefault()
            ?? throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered at damage step {damageStep}");
        occurrence.Trigger(WindowKind.Interrupt, chosen.Card);
        var containing = world.Agenda.Occurrence
            ?? throw new InvalidOperationException(
                $"damage step {damageStep} has no containing occurrence");
        if (!ReferenceEquals(containing, occurrence))
            containing.Trigger(WindowKind.Interrupt, chosen.Card);
        events.AddRange(abilities.Resolve(world, occurrence, chosen,
            input.Spent, input.Targets, input.DefinedValues, input.Allocated));
        world.Agenda.ThenContinuation(step with
        {
            What = resumeStep,
            ProcedureAbilities = [.. pending.Where(ability => ability != chosen)],
            ProcedurePlayersPassed = [], Tier = chosen.Type, OccurrenceId = null,
        }, containing);
    }

    internal static void ResumeCardDefeatedAbility(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 7 has no occurrence");
        var carried = (step.ProcedureAbilities ?? []).ToList();
        if (carried.Count == 1 && AbilityTypes.IsMandatory(carried[0].Type))
        {
            var next = carried[0];
            var containing = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "damage step 7 resume has no containing occurrence");
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            if (!ReferenceEquals(containing, occurrence))
            {
                containing.Trigger(WindowKind.Interrupt, next.Card);
            }
            events.AddRange(abilities.Resolve(world, occurrence, next, [], []));
            world.Agenda.ThenContinuation(step with
            {
                ProcedureAbilities = [],
                ProcedurePlayersPassed = [],
                Tier = next.Type,
                OccurrenceId = null,
            }, containing);
            return;
        }
        var pending = carried.Count > 0
            ? carried
            : FirstInterruptTier(
                abilities, world, occurrence, excludeCard: step.Subject,
                after: step.Tier);
        if (pending.Count > 0)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseCardDefeatedAbility,
                ProcedureAbilities = pending,
                ProcedurePlayersPassed = [],
                OccurrenceId = null,
            });
            return;
        }

        FinalizeCardDefeat(world, facts, step, events);
    }

    private static List<PendingAbility> FirstInterruptTier(
        IWindowAbilities abilities, World world, Occurrence occurrence,
        int excludeCard = -1, AbilityType? after = null)
    {
        var tiers = AbilityWindow.Tiers(
            abilities.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(ability => ability.Card != excludeCard),
            WindowKind.Interrupt,
            occurrence);
        var tier = tiers.FirstOrDefault(candidate => after is null
            || candidate.Priority > AbilityTypes.PriorityOf(after.Value));
        if (tier.Abilities is null)
        {
            return [];
        }
        var (forced, optional) = AbilityWindow.Split(tier);
        return forced.Count > 0 ? [.. forced] : [.. optional];
    }

    private static int ProcedurePlayer(
        World world, PhaseStep step, IReadOnlyList<PendingAbility> pending,
        bool mandatory)
    {
        if (mandatory)
        {
            return world.FirstPlayer;
        }
        var passed = (step.ProcedurePlayersPassed ?? []).ToHashSet();
        return world.PlayerOrder.FirstOrDefault(player =>
            !passed.Contains(player)
            && pending.Any(ability => ability.Player < 0 || ability.Player == player),
            step.Seat >= 0 ? step.Seat : world.FirstPlayer);
    }

    private static bool HasOptionalProcedurePlayer(
        World world, IReadOnlyList<PendingAbility> pending, IReadOnlyList<int> passed)
    {
        var skipped = passed.ToHashSet();
        return world.PlayerOrder.Any(player =>
            !skipped.Contains(player)
            && pending.Any(ability => ability.Player < 0 || ability.Player == player));
    }

    private static void FinalizeCardDefeat(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        if (facts.Kind(card.FaceId) == CardKind.EncounterSideScheme)
        {
            Defeat.FinalizeScheme(world, facts, card, step.ProcedureTrigger, events);
        }
        else
        {
            Defeat.FinalizeCharacter(world, facts, card, step.ProcedureTrigger, events);
        }
    }

    internal static void FinalizeCharacter(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeCharacter(
            world, facts, world.Cards[step.Subject], step.Trigger, events);

    internal static void FinalizeScheme(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeScheme(
            world, facts, world.Cards[step.Subject], step.Trigger, events);
}
