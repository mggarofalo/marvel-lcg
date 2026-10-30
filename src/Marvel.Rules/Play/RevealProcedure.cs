using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves encounter entry and player-owned reveal ordering choices.</summary>

internal static class RevealProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        if (EncounterRevealStages.TryApply(world, facts, step, events)) return null;
        switch (step.What)
        {
            case Steps.ChooseAttachmentTarget:
                return AttachmentTargetPrompt.Describe(world, facts, step);
            case Steps.ChooseRevealAbility:
                return RevealOrderPrompt.Describe(world, facts, step);
            case Steps.ResumeRevealAbility:
                ResumeRevealAbility(world, facts, world.RevealAbilities, step, events);
                break;
            case Steps.ChoosePostRevealAbility:
                return ChoosePostRevealAbility(world, step);
            case Steps.FinalizeAllyEntry:
                FinalizeAllyEntry(world, facts, step.Subject, step.Require<RevealStageOperation>().AllyIncarnation, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the reveal procedure has no step '{step.What}'");
        }
        return null;
    }

    internal static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseAttachmentTarget:
                AttachRevealedCard(
                    world, facts, world.RevealAbilities, step, input, events);
                break;
            case Steps.ChooseRevealAbility:
                ResolveRevealAbility(
                    world, facts, world.RevealAbilities, step, input, events);
                break;
            case Steps.ChoosePostRevealAbility:
                ResolvePostRevealAbility(world, facts, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"reveal step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    internal static void ResolveRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var pending = step.Require<RevealAbilityOperation>().Abilities ?? [];
        var handles = step.Require<RevealAbilityOperation>().Candidates ?? [];
        int chosenIndex = Enumerable.Range(0, handles.Count)
            .FirstOrDefault(index => handles[index] == input.Affordance, -1);
        if (input.IsDecline || chosenIndex < 0 || chosenIndex >= pending.Count)
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered for reveal ordering");
        }

        var occurrence = step.Require<RevealAbilityOperation>().Origin
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        var containingOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("reveal ordering has no containing occurrence");
        ResolveOneRevealAbility(
            world, facts, abilities, step, pending[chosenIndex], events);

        var remainingAbilities = pending.Where((_, index) => index != chosenIndex).ToList();
        var remainingHandles = handles.Where((_, index) => index != chosenIndex).ToList();
        var chosen = pending[chosenIndex];
        world.Agenda.ThenContinuation(step with
        {
            Operation = step.Require<RevealAbilityOperation>() with
            {
                Kind = RevealAbilityKind.ResumeRevealAbility,
                Abilities = remainingAbilities,
                Candidates = remainingHandles,
                Completed = new PendingAbility(chosen.Card, chosen.Type, step.Seat, chosen.Ordinal)
            },
            OccurrenceId = null
        }, containingOccurrence);
    }

    internal static void ResumeRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var occurrence = step.Require<RevealAbilityOperation>().Origin
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        if (step.Require<RevealAbilityOperation>().Completed is { } completed)
            occurrence.Resolution.Complete(completed);

        var pending = step.Require<RevealAbilityOperation>().Abilities ?? [];
        var handles = step.Require<RevealAbilityOperation>().Candidates ?? [];
        int repeatedIndex = RepeatedIndex(step.Require<RevealAbilityOperation>(), pending);
        if (pending.Distinct().Count() > 1 && repeatedIndex < 0)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                Operation = step.Require<RevealAbilityOperation>() with
                {
                    Kind = RevealAbilityKind.ChooseRevealAbility,
                    Completed = null
                },
                OccurrenceId = null
            });
            return;
        }

        if (pending.Count > 0)
        {
            int nextIndex = repeatedIndex >= 0 ? repeatedIndex : 0;
            var next = pending[nextIndex];
            var containingOccurrence = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "reveal continuation has no containing occurrence");
            ResolveOneRevealAbility(
                world, facts, abilities, step, next, events);
            world.Agenda.ThenContinuation(step with
            {
                Operation = step.Require<RevealAbilityOperation>() with
                {
                    Kind = RevealAbilityKind.ResumeRevealAbility,
                    Abilities = pending.Where((_, index) => index != nextIndex).ToList(),
                    Candidates = handles.Where((_, index) => index != nextIndex).ToList(),
                    Completed = new PendingAbility(next.Card, next.Type, step.Seat, next.Ordinal)
                },
                OccurrenceId = null
            }, containingOccurrence);
            return;
        }

        EncounterRevealProcedure.FinishEncounterRevealTail(
            world, world.Cards[step.Subject], step.Seat, step,
            occurrence, events, beforeResponses: false);
    }

    private static int RepeatedIndex(RevealAbilityOperation operation, IReadOnlyList<PendingAbility> pending) =>
        operation.Completed is { } completed ? pending.ToList().IndexOf(completed) : -1;

    private static void ResolveOneRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        PendingAbility ability, List<GameEvent> events)
    {
        Occurrence occurrence = step.Require<RevealAbilityOperation>().Origin
            ?? throw new InvalidOperationException("a revealed ability has no occurrence");
        if (ability.Ordinal is Reveal.InciteResolutionOrdinal or Reveal.SurgeResolutionOrdinal)
        {
            RevealKeywords.ResolveKeyword(
                world, facts, abilities, step.Seat, ability, events, occurrence);
            return;
        }

        ActivityBatches.Append(events, abilities.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], [])).Events);
    }

    internal static Prompt ChoosePostRevealAbility(World world, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        return new Prompt(
            world.FirstPlayer,
            Question.Order,
            TimingPriority.ForcedResponse,
            Steps.CardRevealed,
            $"order responses after {card.FaceId} is revealed",
            false,
            [
                new Affordance(1, "Resolve", card.ObjectId, world.FirstPlayer, "Quickstrike"),
                new Affordance(2, "Resolve", card.ObjectId, world.FirstPlayer, "Teamwork"),
            ]);
    }

    internal static void ResolvePostRevealAbility(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        if (input.IsDecline || input.Affordance is not (1 or 2))
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered after reveal");
        }

        var card = world.Cards[step.Subject];
        if (input.Affordance == 1)
        {
            MinionEntryActivations.Quickstrike(world, facts, card, step.Seat, step.Round);
            MinionEntryActivations.Teamwork(world, facts, card, step.Seat, step.Round);
        }
        else
        {
            MinionEntryActivations.Teamwork(world, facts, card, step.Seat, step.Round);
            MinionEntryActivations.Quickstrike(world, facts, card, step.Seat, step.Round);
        }

        EncounterRevealDiscard.Schedule(
            world, facts, card, step.Seat, step,
            step.Require<PostRevealOperation>().Origin
                ?? throw new InvalidOperationException("post-reveal order has no occurrence"));
    }

    internal static void AttachRevealedCard(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var candidates = step.Require<AttachmentTargetOperation>().Candidates ?? [];
        if (input.IsDecline || !candidates.Contains(input.Affordance))
        {
            throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as an attachment target");
        }

        var card = world.Cards[step.Subject];
        var occurrence = step.Require<AttachmentTargetOperation>().Origin
            ?? world.Agenda.Occurrence
            ?? throw new InvalidOperationException("an attachment choice has no reveal occurrence");
        Reveal.Resolve(
            world, facts, card, step.Seat, events, occurrence,
            new CardEntryPlacement(AttachmentTarget: input.Affordance));
        EncounterRevealProcedure.FinishEncounterReveal(
            world, abilities, card, step.Seat, step, occurrence, events);
    }

    internal static void FinalizeAllyEntry(
        World world, ICardFacts facts, int allyId, int? incarnation,
        List<GameEvent> events)
    {
        var ally = world.Cards[allyId];
        if (ally.Area.Type == DeckType.AlliesArea && ally.Incarnation == incarnation)
        {
            int controller = ally.Area.PlayArea.Player;
            if (CardEntry.CheckAllyLimit(world, facts, controller, allyId))
                CardEntry.FinalizeAllyEntry(world, ally, controller);
            else
                Reveal.EnterPlay(world, facts, ally, events, abilities: world.CardPlayAbilities);
        }
    }
}
