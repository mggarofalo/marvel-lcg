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

internal static class CoreTranscriptPromptAssertions
{
    internal static void HandCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Hand.Cards.Count,
            "cards in hand", step);

    internal static void MulliganOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.Mulligan
            || game.Active != seat
            || game.Pending is not { Cancellable: false } prompt
            || !prompt.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} was not offered its mulligan");
        }
    }

    internal static void ActivePlayer(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.PlayerTurn
            || game.Active != seat
            || game.Pending is not { Player: var player }
            || player != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} is not the active player");
        }
    }

    internal static void EndPhaseDiscardOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        if (game.Phase != GamePhase.EndPhase
            || game.Active != seat
            || game.Pending is not { Player: var player } prompt
            || player != seat
            || !prompt.Affordances.Any(option => option.Verb == Game.EndPhaseVerb))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not offered the end-of-player-phase discard");
        }
    }

    internal static void PendingOrderCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || targets.Legal.Count != count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} cards");
        }
    }

    internal static void SimultaneousEffectChoiceCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Asking: Question.Order } prompt
            || prompt.Player != seat
            || prompt.Affordances.Count != count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} "
                + "simultaneous effects");
        }
    }

    internal static void PendingChoiceCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || !targets.AllowRepeated && targets.Legal.Count < count)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to choose {count} cards");
        }
    }

    internal static void PendingChoiceRange(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int min = Number(match, "min", step);
        int max = Number(match, "max", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != min
            || targets.Max != max
            || !targets.AllowRepeated && targets.Legal.Count < max)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to choose between "
                + $"{min} and {max} cards; actual request was "
                + (context.PendingPrompt is { Affordances.Count: > 0 } pending
                    && pending.Affordances[0].Targets is { } actual
                    ? $"{actual.Min} to {actual.Max} from {actual.Legal.Count} legal cards"
                    : "not a card-target request"));
        }
    }

    internal static void PendingPlayerOrderCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        if (context.PendingPrompt is not { Player: var player, Affordances.Count: 1 } prompt
            || player != seat
            || prompt.Affordances[0].Targets is not { } targets
            || targets.Min != count
            || targets.Max != count
            || targets.Legal.Count != count
            || targets.Legal.Any(id => context.World.Seats.All(
                candidate => candidate.IdentityCard.ObjectId != id)))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1} was not asked to order {count} players");
        }
    }

    internal static void CardInPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Hand))
        {
            throw new TranscriptException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s hand; "
                + $"was {card.Area.Type}");
        }
    }

    internal static void CardInPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Deck))
        {
            throw new TranscriptException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s deck; "
                + $"was {card.Area.Type}");
        }
    }

    internal static void SetupDeckShuffled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.LastSetupDeckShuffle is not { } shuffle
            || shuffle.Seat != seat)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no setup shuffle was observed for seat {seat + 1}");
        }
        if (shuffle.Unshuffled.SequenceEqual(shuffle.After))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1}'s deck retained its unshuffled order");
        }
    }

    internal static void PlayerDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Deck.Cards.Count,
            "cards in the player deck", step);

    internal static void PlayerDeckTopFace(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        IReadOnlyList<Card> deck = context.World.Seats[seat].Deck.Cards;
        Card? top = deck.Count == 0 ? null : deck[^1];
        string expected = match.Groups["face"].Value;
        if (top is null || !string.Equals(top.FaceId, expected, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} on top of seat {seat + 1}'s deck; "
                + $"was {top?.FaceId ?? "<empty>"}");
        }
    }

    internal static void PlayerDiscardCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DiscardPile,
                PlayArea.Of(seat),
                cardOwner: seat).Cards.Count,
            "cards in the discard pile", step);
    }

    internal static void EncounterCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DealtEncounterCardsDeck,
                PlayArea.Of(Seat(match, step))).Cards.Count,
            "facedown encounter cards", step);

    internal static void FacedownEncounterQueueCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.FaceUp
            || card.Area.Type != DeckType.DealtEncounterCardsDeck
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} facedown in seat "
                + $"{seat + 1}'s encounter queue; was {card.Area}, faceup={card.FaceUp}");
        }
    }

    internal static void EncounterDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDeck).Cards.Count,
            "cards in the encounter deck", step);

    internal static void PlayerCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step), context.World.Players,
            "players in the game", step);

    internal static void EncounterDeckFaceCounts(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        TranscriptTable table = Table(step, "card", "count");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string face = row["card"];
            int expected = TableNumber(row, "count", step);
            int actual = context.World.AreaOf(DeckType.EncounterDeck).Cards.Count(
                card => card.FaceId == face);
            Equal(expected, actual, $"copies of {face} in the encounter deck", step);
        }
    }

    internal static void OwnedPlayerCardCounts(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "card", "count");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string face = row["card"];
            int expected = TableNumber(row, "count", step);
            int actual = context.World.Cards.Count(card =>
                card.Owner == seat && card.Faces.Contains(face, StringComparer.Ordinal));
            Equal(expected, actual, $"owned copies of {face}", step);
        }
    }

}
