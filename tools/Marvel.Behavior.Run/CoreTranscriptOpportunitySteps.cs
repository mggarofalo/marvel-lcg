using System.Text.RegularExpressions;
using Marvel.Content.Behavior;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

/// <summary>Transcript answers and assertions at optional ability windows.</summary>
internal static class CoreTranscriptOpportunitySteps
{
    internal static void AcceptPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending opportunity asks seat {asked.Player + 1}, "
                + $"not seat {seat + 1}");
        }

        string label = match.Groups["label"].Value;
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.Label == label)
            ?? throw new TranscriptException(
                $"{step.Location}: '{label}' is not offered by '{asked.Label}'");
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void AcceptCardPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending opportunity asks seat {asked.Player + 1}, "
                + $"not seat {seat + 1}");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == card.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} is not offered by '{asked.Label}'");
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void DeclinePendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        if (asked.Player != seat || !asked.Cancellable)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} cannot decline '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Decline,
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void PendingOpportunityOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no opportunity is pending");
        string label = match.Groups["label"].Value;
        if (asked.Player != seat || !asked.Affordances.Any(candidate =>
                candidate.Label == label))
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not offered '{label}'");
        }
    }

    internal static void PendingWindowPass(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.PendingPrompt is not { Cancellable: true } asked
            || asked.Player != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} cannot pass the pending window");
        }
    }

    internal static void NoPendingOpportunity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.PendingPrompt is not null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: '{context.PendingPrompt.Label}' is still pending");
        }
    }

}
