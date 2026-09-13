using System.Text.RegularExpressions;
using Marvel.Content.Behavior;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptEnemyActions
{
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
}
