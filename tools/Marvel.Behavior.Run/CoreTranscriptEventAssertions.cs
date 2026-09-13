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

internal static class CoreTranscriptEventAssertions
{
    internal static void EncounterDiscardCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDiscardPile).Cards.Count,
            "cards in the encounter discard pile", step);

    internal static void AccelerationTokenCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        long actual = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        Equal(Number(match, "count", step), checked((int)actual),
            "acceleration tokens on the main scheme", step);
    }

    internal static void CardReadiness(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = string.Equals(
            match.Groups["state"].Value, "ready", StringComparison.Ordinal);
        if (card.Ready != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be "
                + $"{match.Groups["state"].Value}");
        }
    }

    internal static void GeneratedResources(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string expected = match.Groups["resources"].Value;
        if (context.LastResourceGeneration is not { } actual
            || actual.Card != card.ObjectId
            || !string.Equals(actual.Resources, expected, StringComparison.Ordinal))
        {
            string observed = context.LastResourceGeneration is { } generation
                ? $"card {generation.Card} generated {generation.Resources}"
                : "no resource ability was used";
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to generate {expected}; "
                + observed);
        }
    }

    internal static void LastAttackDefense(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (context.World.FinishedAttack is not { } attack
            || attack.Defender != card.ObjectId
            || attack.BasicDefense)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} did not defend the last attack "
                + "with a defense-labeled ability");
        }
    }

    internal static void LastAttackUndefended(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.FinishedAttack is not { Defender: < 0 })
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: the last attack was defended");
        }
    }

    internal static void CardInPlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Area expected = context.World.AreaOf(
            DeckType.DiscardPile,
            PlayArea.Of(seat),
            cardOwner: seat);
        if (!ReferenceEquals(card.Area, expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s "
                + $"discard pile; was {card.Area.Type} in play area "
                + $"{card.Area.PlayArea.Player} (owner {card.Area.CardOwner}, "
                + $"host {card.Area.Host})");
        }
    }

    internal static void CardOnTopOfPlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        AssertFaceupTop(
            context,
            step,
            match,
            context.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat));
    }

    internal static void CardOnTopOfEncounterDiscard(
        TranscriptContext context, TranscriptStep step, Match match) =>
        AssertFaceupTop(
            context, step, match, context.World.AreaOf(DeckType.EncounterDiscardPile));

    internal static void CardInEncounterDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.EncounterDiscardPile)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in the encounter "
                + $"discard pile; was {card.Area.Type}");
        }
    }

    internal static void CardResolving(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.RevealingArea || !card.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {card.ObjectId} in the resolving area; "
                + $"was {card.Area}, faceup={card.FaceUp}");
        }
    }

    internal static void AssertFaceupTop(
        TranscriptContext context, TranscriptStep step, Match match, Area area)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = area.Cards.Count == 0 ? null : area.Cards[^1];
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} on top of "
                + $"{area}; was {(actual is null ? "<empty>" : actual.ObjectId)}");
        }
    }

    internal static void PlayerDiscardOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "card", "copy");
        int[] expected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        int[] actual = [.. context.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat)
            .Cards.AsEnumerable().Reverse().Take(expected.Length)
            .Select(card => card.ObjectId)];
        if (!actual.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected discard top order "
                + $"{string.Join(',', expected)}; was {string.Join(',', actual)}");
        }
    }

    internal static void EventCardOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        int[] expected = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        int[] actual = [.. context.Events
            .OfType<CardsMoved>()
            .Where(moved => moved.Verb == match.Groups["verb"].Value)
            .SelectMany(moved => moved.Cards)
            .Select(landing => landing.Card)];
        if (!actual.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["verb"].Value} card order "
                + $"{string.Join(',', expected)}; was {string.Join(',', actual)}");
        }
    }

    internal static void EventEmitted(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string verb = match.Groups["verb"].Value;
        string trigger = match.Groups["trigger"].Value;
        if (!context.Events.Any(gameEvent =>
                gameEvent.Verb == verb && gameEvent.Trigger == trigger))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no {verb} event with trigger {trigger} was emitted");
        }
    }

    internal static void EventCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            context.Events.Count(gameEvent =>
                gameEvent.Verb == match.Groups["verb"].Value),
            $"{match.Groups["verb"].Value} events",
            step);

    internal static void BoostCardFlipCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            context.Events.OfType<CardsFlipped>().Where(gameEvent =>
                gameEvent.FaceUp && gameEvent.Trigger == Steps.AttackInitiated)
                .Sum(gameEvent => gameEvent.Cards.Count),
            "cards turned faceup as boost cards",
            step);

    internal static void EventOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int first = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["first"].Value);
        int second = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["second"].Value);
        if (first < 0 || second <= first)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["first"].Value} before "
                + match.Groups["second"].Value);
        }
    }

}
