using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptDeckAndReveal
{
    internal static void DrawCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Draw.Cards(context.World, seat, count, "behavioral transcript", context.Events);
    }

    internal static void DiscardPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = PlayerDeck.DiscardTop(
            context.World, seat, count, "behavioral transcript", context.Events);
    }

    internal static void DiscardFromHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        var reference = new SceneCard(
            match.Groups["face"].Value, Number(match, "copy", step));
        Card card = context.SceneRequired(step).Find(reference);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Hand))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} is not in seat {seat + 1}'s hand");
        }

        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Discard.Card(context.World, card, "behavioral transcript", context.Events);
    }

    internal static void DiscardCardEffect(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Discard.Card(context.World, card, "behavioral transcript effect", context.Events);
    }

    internal static void DealEncounterCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        for (int index = 0; index < count; index++)
        {
            _ = Deal.EncounterCard(
                context.World, seat, "behavioral transcript", context.Events);
        }
    }

    internal static void DiscardEncounterDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = EncounterDeck.DiscardTop(
            context.World,
            Number(match, "count", step),
            "behavioral transcript",
            context.Events);
    }

    internal static void RevealEncounterCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Area queue = context.World.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat));
        World.MoveToTop(card, queue);
        context.World.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, Round: 1, Number: 4,
            Subject: card.ObjectId, Seat: seat));
        context.Events.Clear();
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void RevealEncounterCardWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Card defender = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["defender"].Value,
            Number(match, "defenderCopy", step)));
        Area queue = context.World.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat));
        World.MoveToTop(card, queue);
        context.World.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, Round: 1, Number: 4,
            Subject: card.ObjectId, Seat: seat));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        FinishWithDefender(
            context, step, defender, acceptedLabel: null,
            stopAtRequiredAfterDefense: true);
    }

    internal static void RevealEncounterCardWithDefenderAndOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Card defender = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["defender"].Value,
            Number(match, "defenderCopy", step)));
        Area queue = context.World.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat));
        World.MoveToTop(card, queue);
        context.World.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, Round: 1, Number: 4,
            Subject: card.ObjectId, Seat: seat));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        FinishWithDefender(context, step, defender, match.Groups["label"].Value);
    }

    internal static void AssignThreatAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Schedule(
            context.World,
            scheme,
            villain,
            Number(match, "count", step),
            ThreatCause.CardAbility,
            "behavioral transcript",
            Seat(match, step));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    internal static void BeginThreatAssignment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Schedule(
            context.World,
            scheme,
            villain,
            Number(match, "count", step),
            ThreatCause.EnemyScheme,
            "behavioral transcript",
            Seat(match, step));
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void AnswerEncounterCard(
        TranscriptContext context, TranscriptStep step, Match match)
        => AnswerEncounterCard(context, step, match, payments: []);

    internal static void AnswerEncounterCardWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        int[] payments = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        AnswerEncounterCard(context, step, match, payments);
    }

    internal static void AnswerEncounterCard(
        TranscriptContext context, TranscriptStep step, Match match, int[] payments)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException(
                $"{step.Location}: no encounter-card decision is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: the pending encounter-card decision belongs to "
                + $"seat {asked.Player + 1}, not seat {seat + 1}");
        }

        int option = Number(match, "option", step);
        if (option < 1 || option > asked.Affordances.Count)
        {
            throw new TranscriptException(
                $"{step.Location}: option {option} is outside the pending decision's "
                + $"{asked.Affordances.Count} choices");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(asked.Affordances[option - 1].Id, [], payments),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void OrderPendingPlayers(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException(
                $"{step.Location}: no encounter-card decision is pending");
        if (asked.Player != seat || asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending player order does not belong to seat {seat + 1}");
        }

        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException(
                $"{step.Location}: pending encounter-card decision asks for no order");
        TranscriptTable table = Table(step, "seat");
        int[] ordered = [.. table.Rows.Select(row =>
            context.World.Seats[TableNumber(row, "seat", step) - 1].IdentityCard.ObjectId)];
        if (ordered.Length < targets.Min
            || ordered.Length > targets.Max
            || ordered.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested player order is not legal for '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, ordered, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void PhaseDiscardNone(
        TranscriptContext context, TranscriptStep step, Match match) =>
        PhaseDiscard(context, step, Seat(match, step), []);

    internal static void PhaseDiscardSelected(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        PhaseDiscard(
            context,
            step,
            Seat(match, step),
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(
                new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    internal static void PhaseDiscard(
        TranscriptContext context,
        TranscriptStep step,
        int seat,
        IReadOnlyList<int> cards)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DiscardToHandSize(
            context.World, context.Cards, seat, cards, context.Events);
    }

    internal static void PhaseDraw(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DrawToHandSize(context.World, context.Cards, context.Events);
    }

    internal static void PhaseReady(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.ReadyCards(context.World, context.Events);
    }

    internal static void EndPlayerPhase(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DrawToHandSize(context.World, context.Cards, context.Events);
        PhaseEnd.ReadyCards(context.World, context.Events);
        PhaseEnd.EndPlayerPhase(context.World, context.Events);
    }

    internal static void EndVillainPhaseAndRound(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.EndVillainPhase(context.World, context.Cards, context.Events);
    }

}
