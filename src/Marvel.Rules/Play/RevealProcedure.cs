using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class RevealProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.DealEncounterCards:
                EncounterRevealProcedure.DealEncounterCards(world, facts, events);
                break;
            case Steps.RevealEncounterCards:
                EncounterRevealProcedure.RevealNextEncounterCard(world, step);
                break;
            case Steps.RevealEncounterCard:
                EncounterRevealProcedure.RevealEncounterCard(
                    world, facts, world.RevealAbilities, world.Cards[step.Subject],
                    step.Seat, step.Round, events);
                break;
            case Steps.DiscardRevealedTreachery:
                EncounterRevealProcedure.DiscardRevealedTreachery(
                    world, facts, step, events);
                break;
            case Steps.ChooseAttachmentTarget:
                return DefeatProcedure.ChooseAttachmentTarget(world, facts, step);
            case Steps.ChooseRevealAbility:
                return ChooseRevealAbility(world, facts, step);
            case Steps.ResumeRevealAbility:
                ResumeRevealAbility(world, facts, world.RevealAbilities, step, events);
                break;
            case Steps.ChoosePostRevealAbility:
                return ChoosePostRevealAbility(world, step);
            case Steps.FinalizeAllyEntry:
                FinalizeAllyEntry(world, facts, step.Subject, step.Seat, events);
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

    internal static Prompt ChooseRevealAbility(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var handles = step.ProcedureCandidates ?? [];
        var abilities = step.ProcedureAbilities ?? [];
        if (handles.Count != abilities.Count || handles.Count < 2)
        {
            throw new InvalidOperationException("reveal ordering has no simultaneous abilities");
        }

        return new Prompt(
            world.FirstPlayer,
            Question.Order,
            TimingPriority.Occurrence,
            Steps.CardRevealed,
            $"order When Revealed abilities on {card.FaceId}",
            false,
            [.. handles.Select((handle, index) => new Affordance(
                handle,
                "Resolve",
                card.ObjectId,
                world.FirstPlayer,
                RevealAbilityLabel(facts, card, abilities[index])))]);
    }

    private static string RevealAbilityLabel(
        ICardFacts facts, Card card, PendingAbility ability) => ability.Ordinal switch
        {
            Reveal.InciteResolutionOrdinal => "Incite",
            Reveal.SurgeResolutionOrdinal => "Surge",
            _ => $"{facts.Title(card.FaceId)} When Revealed {ability.Ordinal + 1}",
        };

    internal static void ResolveRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var pending = step.ProcedureAbilities ?? [];
        var handles = step.ProcedureCandidates ?? [];
        int chosenIndex = Enumerable.Range(0, handles.Count)
            .FirstOrDefault(index => handles[index] == input.Affordance, -1);
        if (input.IsDecline || chosenIndex < 0 || chosenIndex >= pending.Count)
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered for reveal ordering");
        }

        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        var containingOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("reveal ordering has no containing occurrence");
        ResolveOneRevealAbility(
            world, facts, abilities, world.Cards[step.Subject], step.Seat,
            pending[chosenIndex], occurrence, events);

        var remainingAbilities = pending.Where((_, index) => index != chosenIndex).ToList();
        var remainingHandles = handles.Where((_, index) => index != chosenIndex).ToList();
        var chosen = pending[chosenIndex];
        world.Agenda.ThenContinuation(step with
        {
            What = Steps.ResumeRevealAbility,
            ProcedureAbilities = remainingAbilities,
            ProcedureCandidates = remainingHandles,
            ProcedureSource = chosen.Card,
            Tier = chosen.Type,
            AbilityOrdinal = chosen.Ordinal,
            OccurrenceId = null,
        }, containingOccurrence);
    }

    internal static void ResumeRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        if (step.ProcedureSource >= 0
            && step.AbilityOrdinal >= 0
            && step.Tier is { } completedType)
        {
            occurrence.Complete(new PendingAbility(
                step.ProcedureSource, completedType, step.Seat, step.AbilityOrdinal));
        }

        var pending = step.ProcedureAbilities ?? [];
        var handles = step.ProcedureCandidates ?? [];
        if (pending.Count > 1)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseRevealAbility,
                ProcedureSource = -1,
                Tier = null,
                AbilityOrdinal = -1,
                OccurrenceId = null,
            });
            return;
        }

        if (pending.Count == 1)
        {
            var next = pending[0];
            var containingOccurrence = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "reveal continuation has no containing occurrence");
            ResolveOneRevealAbility(
                world, facts, abilities, world.Cards[step.Subject], step.Seat,
                next, occurrence, events);
            world.Agenda.ThenContinuation(step with
            {
                What = Steps.ResumeRevealAbility,
                ProcedureAbilities = [],
                ProcedureCandidates = [],
                ProcedureSource = next.Card,
                Tier = next.Type,
                AbilityOrdinal = next.Ordinal,
                OccurrenceId = null,
            }, containingOccurrence);
            return;
        }

        EncounterRevealProcedure.FinishEncounterRevealTail(
            world, facts, world.Cards[step.Subject], step.Seat, step.Round,
            occurrence, events, beforeResponses: false);
    }

    private static void ResolveOneRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        PendingAbility ability, Occurrence occurrence, List<GameEvent> events)
    {
        if (ability.Ordinal is Reveal.InciteResolutionOrdinal or Reveal.SurgeResolutionOrdinal)
        {
            RevealKeywords.ResolveKeyword(
                world, facts, abilities, card, player, ability, events, occurrence);
            return;
        }

        events.AddRange(abilities.Resolve(world, occurrence, ability, [], []));
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
            Reveal.Quickstrike(world, facts, card, step.Seat, step.Round);
            Reveal.Teamwork(world, facts, card, step.Seat, step.Round);
        }
        else
        {
            Reveal.Teamwork(world, facts, card, step.Seat, step.Round);
            Reveal.Quickstrike(world, facts, card, step.Seat, step.Round);
        }

        EncounterRevealProcedure.FinishEncounterRevealDiscard(
            world, facts, card, step.Seat, step.Round,
            step.ProcedureOccurrence
                ?? throw new InvalidOperationException("post-reveal order has no occurrence"));
    }

    internal static void AttachRevealedCard(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var candidates = step.ProcedureCandidates ?? [];
        if (input.IsDecline || !candidates.Contains(input.Affordance))
        {
            throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as an attachment target");
        }

        var card = world.Cards[step.Subject];
        var occurrence = step.AbilityOccurrence
            ?? world.Agenda.Occurrence
            ?? throw new InvalidOperationException("an attachment choice has no reveal occurrence");
        Reveal.Resolve(
            world, facts, card, step.Seat, events, occurrence, input.Affordance);
        EncounterRevealProcedure.FinishEncounterReveal(
            world, facts, abilities, card, step.Seat, step.Round, occurrence, events);
    }

    internal static void FinalizeAllyEntry(
        World world, ICardFacts facts, int allyId, int player,
        List<GameEvent> events)
    {
        var ally = world.Cards[allyId];
        if (ally.Area.Type == DeckType.AlliesArea)
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: world.CardPlayAbilities);
            CardEntry.Entered(world, ally, player);
        }
    }

}
