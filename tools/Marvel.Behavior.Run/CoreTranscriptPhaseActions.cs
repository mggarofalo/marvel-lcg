using System.Text.RegularExpressions;
using Marvel.Content.Behavior;
using Marvel.Rules.Play;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptPhaseActions
{
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
