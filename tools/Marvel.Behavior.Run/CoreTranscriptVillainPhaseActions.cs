using System.Text.RegularExpressions;
using Marvel.Content.Behavior;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptVillainPhaseActions
{
    internal static void ResolveVillainPhase(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(context.World.Agenda, Number(match, "round", step));
        FinishAgenda(context, step);
    }

    internal static void ResolveVillainPhaseUntilRequired(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(context.World.Agenda, Number(match, "round", step));
        FinishUntilRequired(context, step);
    }

    internal static void ResolveVillainPhaseAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        VillainPhase.Schedule(context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    internal static void ResolveVillainPhaseAcceptingWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card payment = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(context.World.Agenda, Number(match, "round", step));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId);
    }

    internal static void ResolveVillainPhaseWithDefender(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card defender = context.SceneRequired(step).Find(SceneCard(match, step));
        VillainPhase.Schedule(context.World.Agenda, Number(match, "round", step));

        FinishWithDefender(
            context, step, defender, acceptedLabel: null,
            stopAtRequiredAfterDefense: true);
    }
}
