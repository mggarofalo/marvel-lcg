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

internal static class CoreTranscriptTurnActions
{
    internal static void InspectCoreScene(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        _ = context.World;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
    }

    internal static void BeginMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.Game = Game.Begin(context.World, context.Cards, context.World.Abilities);
        SetPendingPrompt(context, context.Game.Pending);
        if (context.Game.Phase != GamePhase.Mulligan
            || context.Game.Active != Seat(match, step))
        {
            throw new TranscriptException(
                $"{step.Location}: setup did not reach the requested player's mulligan");
        }
    }

    internal static void ResolveMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not resolving a mulligan");
        }

        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no mulligan prompt is pending");
        Affordance option = asked.Affordances.Single(candidate =>
            candidate.Verb == Game.ResolveMulligans);
        TranscriptTable table = Table(step, "card", "copy");
        int[] selected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id, selected, []));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    internal static void KeepMulligan(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not resolving a mulligan");
        }

        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Decline);
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    internal static void EndPlayerTurn(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerTurn || game.Active != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Decline);
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    internal static void RequestVoluntaryFormChange(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastAvailability = asked.Affordances.Any(option =>
            option.Verb == Game.ChangeForm);
    }

    internal static void RequestCardPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (context.Game is null)
        {
            context.LastAvailability = CardPayment.Price(
                context.World, context.Cards, context.World.Seats[seat], card) is not null;
            return;
        }

        Game game = context.Game;
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastAvailability = asked.Affordances.Any(option =>
            option.Verb == CardPlay.Verb && option.AnchorId == card.ObjectId);
    }

    internal static void TakeVoluntaryFormChange(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seatIndex = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || asked.Player != seatIndex)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seatIndex + 1} is not taking their turn");
        }

        Affordance option = asked.Affordances.Single(candidate =>
            candidate.Verb == Game.ChangeForm);
        Seat seat = context.World.Seats[seatIndex];
        string from = FormName(Forms.Of(context.World, seat, context.Cards));
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
        string to = FormName(Forms.Of(context.World, seat, context.Cards));
        context.LastFormChange = (seatIndex, from, to);
    }

    internal static void ChooseSetupCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerSetup)
        {
            throw new TranscriptException(
                $"{step.Location}: no player Setup ability is resolving");
        }

        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no setup prompt is pending");
        Card selected = context.SceneRequired(step).Find(SceneCard(match, step));
        Affordance option = asked.Affordances.SingleOrDefault(candidate =>
            candidate.AnchorId == selected.ObjectId)
            ?? throw new TranscriptException(
                $"{step.Location}: card {selected.ObjectId} is not offered by '{asked.Label}'");
        int[] unshuffled =
        [
            .. context.World.Seats[seat].Deck.Cards
                .Where(card => card.ObjectId != selected.ObjectId)
                .Select(card => card.ObjectId),
        ];
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(option.Id));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
        context.LastSetupDeckShuffle = (
            seat,
            unshuffled,
            [.. context.World.Seats[seat].Deck.Cards.Select(card => card.ObjectId)]);
    }

}
