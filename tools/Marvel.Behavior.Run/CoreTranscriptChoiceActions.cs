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

internal static class CoreTranscriptChoiceActions
{
    internal static void InitiateAction(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        IReadOnlyList<int> payments,
        IReadOnlyList<int>? chosen = null,
        int? ordinal = null,
        IReadOnlyDictionary<string, long>? values = null)
    {
        int seat = Seat(match, step);
        Card source = context.SceneRequired(step).Find(SceneCard(match, step));
        var runner = (AbilityRunner)context.World.Abilities;
        var actions = runner.Actions(context.World, seat)
            .Where(candidate => candidate.Card == source.ObjectId);
        var action = ordinal is null
            ? actions.Single()
            : actions.Single(candidate => candidate.Ordinal == ordinal.Value);
        CostOption? price = runner.Describe(context.World, action).CostOptions.SingleOrDefault();
        IReadOnlyList<ResourceAllocation>? allocations = price is null
            ? null
            : values is not null
                ? null
                : ResourcePayment.Allocate(price, payments)
                ?? throw new TranscriptException(
                    $"{step.Location}: the selected cards cannot be allocated to the action cost");
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        // Actions are game occurrences, not direct interpreter calls. Routing
        // the accepted action through the agenda preserves its interrupt and
        // response windows when paying the cost itself defeats a character.
        context.World.Agenda.AddPlayerAction(1, new PlayerAction(
            action,
            [.. payments],
            [.. chosen ?? []],
            values is null
                ? null
                : new Dictionary<string, long>(values, StringComparer.Ordinal),
            allocations is null ? null : [.. allocations]));
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, runner, context.Events));
    }

    internal static void ChoosePendingCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        var offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == target.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {target.ObjectId} is not offered by '{asked.Label}'");
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

    internal static void OrderPendingCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        if (asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending order has {asked.Affordances.Count} affordances");
        }
        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException($"{step.Location}: pending action asks for no order");
        TranscriptTable table = Table(step, "card", "copy");
        int[] ordered = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        if (ordered.Length < targets.Min
            || ordered.Length > targets.Max
            || ordered.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested order [{string.Join(',', ordered)}] is not "
                + $"legal for '{asked.Label}' [{string.Join(',', targets.Legal)}]");
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

    internal static void ChoosePendingCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        if (asked.Affordances.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: pending choice has {asked.Affordances.Count} affordances");
        }
        Affordance offer = asked.Affordances[0];
        TargetRequest targets = offer.Targets
            ?? throw new TranscriptException($"{step.Location}: pending action asks for no targets");
        TranscriptTable table = Table(step, "card", "copy");
        int[] selected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        if (selected.Length < targets.Min
            || selected.Length > targets.Max
            || !targets.AllowRepeated && selected.Distinct().Count() != selected.Length
            || selected.Any(id => !targets.Legal.Contains(id)))
        {
            throw new TranscriptException(
                $"{step.Location}: requested targets are not legal for '{asked.Label}'");
        }

        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, selected, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void ChoosePendingCardAndDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        var offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == target.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {target.ObjectId} is not offered by '{asked.Label}'");
        TranscriptTable table = Table(step, "card", "copy");
        int[] discarded = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, discarded, []),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void ChoosePendingCardWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no action prompt is pending");
        if (asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: pending action asks seat {asked.Player + 1}, not seat {seat + 1}");
        }

        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance offer = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == target.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {target.ObjectId} is not offered by '{asked.Label}'");
        TranscriptTable table = Table(step, "card", "copy");
        int[] payments = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        Sequence.Answer(
            context.World,
            context.Cards,
            context.World.Abilities,
            asked,
            Decision.Take(offer.Id, [], payments),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

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

    internal static void PendingOptionAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptException($"{step.Location}: no decision is pending");
        int option = Number(match, "option", step) - 1;
        bool offered = asked.Affordances.Any(candidate => candidate.Id == option);
        bool expected = match.Groups["availability"].Value == "offered";
        if (offered != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: option {option + 1} was "
                + (offered ? "offered" : "not offered"));
        }
    }

    internal static void SetPendingPrompt(TranscriptContext context, Prompt? prompt)
    {
        context.PendingPrompt = prompt;
        context.CurrentPrompt = prompt?.Label ?? "<none>";
    }

}
