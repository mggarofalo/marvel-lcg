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

internal static class CoreTranscriptVillainActions
{
    internal static void ResolveVillainPhase(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgenda(context, step);
    }

    internal static void ResolveVillainPhaseUntilRequired(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishUntilRequired(context, step);
    }

    internal static void ResolveVillainPhaseAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    internal static void ResolveVillainPhaseAcceptingWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card payment = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId);
    }

    internal static void ResolveVillainPhaseWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(
            context.World.Agenda, Number(match, "round", step));

        FinishWithDefender(
            context, step, defender, acceptedLabel: null,
            stopAtRequiredAfterDefense: true);
    }

    internal static void ResolveVillainAttackWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));

        FinishWithDefender(
            context, step, defender, acceptedLabel: null,
            stopAtRequiredAfterDefense: true);
    }

    internal static void ResolveVillainAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgenda(context, step);
    }

    internal static void ResolveVillainAttackUntilRequired(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));

        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null && asked.Cancellable; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: attack still asks '{asked.Label}' after 100 declines");
            }

            Sequence.Answer(
                context.World, context.Cards, context.World.Abilities,
                asked, Decision.Decline, context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }

        if (asked is null)
        {
            throw new TranscriptException(
                $"{step.Location}: attack reached no required decision");
        }

        SetPendingPrompt(context, asked);
    }

    internal static void ResolveEnemyAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card enemy = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: enemy.ObjectId, Seat: seat));
        FinishAgenda(context, step);
    }

    internal static void ResolveEnemyAttackUntilRequired(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card enemy = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: enemy.ObjectId, Seat: seat));
        FinishUntilRequired(context, step);
    }

    internal static void ResolveEnemyAttackWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card enemy = context.SceneRequired(step).Find(SceneCard(match, step));
        Card defender = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["defender"].Value,
            Number(match, "defenderCopy", step)));
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: enemy.ObjectId, Seat: seat));
        FinishWithDefender(context, step, defender, acceptedLabel: null);
    }

    internal static void ResolveVillainScheme(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Scheme, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgenda(context, step);
    }

    internal static void ResolveVillainAttackWithDefenderAndOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));

        FinishWithDefender(context, step, defender, match.Groups["label"].Value);
    }

    internal static void ResolveVillainAttackWithOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    internal static void ResolveVillainAttackWithTwoOpportunities(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        int seat = Seat(match, step);
        context.World.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: seat,
            Subject: villain.ObjectId, Seat: seat));
        FinishAgendaAccepting(
            context, step,
            [match.Groups["first"].Value, match.Groups["second"].Value]);
    }

    internal static void FinishWithDefender(
        TranscriptContext context, TranscriptStep step, Card defender, string? acceptedLabel,
        bool stopAtRequiredAfterDefense = false)
    {

        bool defended = false;
        bool accepted = acceptedLabel is null;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            RequireAgendaProgress(answered, asked, step);
            if (defended && stopAtRequiredAfterDefense && !asked.Cancellable)
            {
                SetPendingPrompt(context, asked);
                return;
            }
            Decision decision = DefenderDecision(
                asked, defender, acceptedLabel, step, ref accepted, ref defended);
            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                decision,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }
        if (!defended)
            throw new TranscriptException(
                $"{step.Location}: the villain phase offered no defender window");
        if (!accepted)
            throw new TranscriptException(
                $"{step.Location}: the attack offered no '{acceptedLabel}' opportunity");
    }

    internal static Decision DefenderDecision(
        Prompt asked, Card defender, string? acceptedLabel, TranscriptStep step,
        ref bool accepted, ref bool defended)
    {
        var opportunity = accepted ? null : asked.Affordances
            .SingleOrDefault(option => option.Label == acceptedLabel);
        if (opportunity is not null)
        {
            accepted = true;
            return Decision.Take(opportunity.Id);
        }
        if (defended || asked.Asking != Question.Defender) return Decision.Decline;
        if (!asked.Affordances.Any(option => option.AnchorId == defender.ObjectId))
            throw new TranscriptException(
                $"{step.Location}: card {defender.ObjectId} was not offered as a defender");
        defended = true;
        return Decision.Take(defender.ObjectId);
    }

    internal static void RequireAgendaProgress(
        int answered, Prompt asked, TranscriptStep step)
    {
        if (answered >= 100)
            throw new TranscriptException(
                $"{step.Location}: agenda still asks '{asked.Label}' after 100 answers");
    }

    internal static void FinishAgenda(TranscriptContext context, TranscriptStep step)
    {
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 declines");
            }

            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                Decision.Decline,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }
    }

    internal static void FinishUntilRequired(
        TranscriptContext context, TranscriptStep step)
    {
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is { Cancellable: true }; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 declines");
            }

            Sequence.Answer(
                context.World, context.Cards, context.World.Abilities,
                asked, Decision.Decline, context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }

        if (asked is null)
        {
            throw new TranscriptException(
                $"{step.Location}: agenda reached no required decision");
        }

        SetPendingPrompt(context, asked);
    }

    internal static void FinishAgendaAccepting(
        TranscriptContext context, TranscriptStep step, string acceptedLabel,
        int? payment = null, int? target = null)
    {
        bool accepted = false;
        bool targetChosen = target is null;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            RequireAgendaProgress(answered, asked, step);
            Decision decision = AcceptanceDecision(
                asked, acceptedLabel, payment, target,
                ref accepted, ref targetChosen);
            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                decision,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }

        if (!accepted)
        {
            throw new TranscriptException(
                $"{step.Location}: the action offered no '{acceptedLabel}' opportunity");
        }
        if (!targetChosen)
        {
            throw new TranscriptException(
                $"{step.Location}: '{acceptedLabel}' offered no requested target {target}");
        }
    }

    internal static Decision AcceptanceDecision(
        Prompt asked, string acceptedLabel, int? payment, int? target,
        ref bool accepted, ref bool targetChosen)
    {
        var opportunity = accepted ? null : asked.Affordances
            .SingleOrDefault(option => option.Label == acceptedLabel);
        if (opportunity is not null)
        {
            accepted = true;
            return payment is null
                ? Decision.Take(opportunity.Id)
                : Decision.Take(
                    opportunity.Id, target is null ? [] : [target.Value],
                    [payment.Value]);
        }
        if (!targetChosen)
        {
            var targetOption = asked.Affordances.SingleOrDefault(option =>
                option.Id == target || option.AnchorId == target);
            if (targetOption is not null)
            {
                targetChosen = true;
                return Decision.Take(targetOption.Id);
            }
        }
        return Decision.Decline;
    }

    internal static void FinishAgendaAccepting(
        TranscriptContext context, TranscriptStep step, IReadOnlyList<string> acceptedLabels)
    {
        int next = 0;
        Prompt? asked = Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events);
        for (int answered = 0; asked is not null; answered++)
        {
            if (answered >= 100)
            {
                throw new TranscriptException(
                    $"{step.Location}: agenda still asks '{asked.Label}' after 100 answers");
            }

            Affordance? opportunity = next == acceptedLabels.Count
                ? null
                : asked.Affordances.SingleOrDefault(option =>
                    option.Label == acceptedLabels[next]);
            Decision decision = opportunity is null
                ? Decision.Decline
                : Decision.Take(opportunity.Id);
            next += opportunity is null ? 0 : 1;
            Sequence.Answer(
                context.World,
                context.Cards,
                context.World.Abilities,
                asked,
                decision,
                context.Events);
            asked = Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events);
        }

        if (next != acceptedLabels.Count)
        {
            throw new TranscriptException(
                $"{step.Location}: the attack offered no '{acceptedLabels[next]}' opportunity");
        }
    }

}
