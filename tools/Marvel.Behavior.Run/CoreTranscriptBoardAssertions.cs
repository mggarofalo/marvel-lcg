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

internal static class CoreTranscriptBoardAssertions
{
    internal static void FirstPlayer(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Seat(match, step), context.World.FirstPlayer, "first-player seat", step);

    internal static void MinionEngaged(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.EngagedEnemiesArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} engaged with seat "
                + $"{seat + 1}; was {card.Area}");
        }
    }

    internal static void FacedownDroneCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int actual = FacedownDrones.EngagedWith(
            context.World, Seat(match, step)).Count;
        int expected = Number(match, "count", step);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} facedown Drone minions; was {actual}");
        }
    }

    internal static void AllyControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.AlliesArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} as an ally controlled by "
                + $"seat {seat + 1}; was {card.Area}");
        }
    }

    internal static void SupportControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.SupportsArea
            || card.Area.PlayArea != PlayArea.Of(seat))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} as a support controlled by "
                + $"seat {seat + 1}; was {card.Area}");
        }
    }

    internal static void CardDamage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            checked((int)context.SceneRequired(step).Find(SceneCard(match, step)).Damage),
            "damage on the card",
            step);

    internal static void ModifiedCardStatistic(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string field = match.Groups["field"].Value switch
        {
            "ATK" => "attack",
            "DEF" => "defense",
            "HS" => "hand_size",
            "REC" => "recover",
            "THW" => "thwart",
            _ => throw new InvalidOperationException("unsupported printed statistic"),
        };
        int expected = Number(match, "count", step);
        int actual = checked((int)StateFields.Modified(
            context.World, card, field, context.Cards, context.World.Players));
        if (expected != actual)
        {
            string effects = string.Join(", ", context.World.Effects.Active()
                .Where(effect => effect.Kind == field)
                .Select(effect => $"{effect.Kind}:{effect.Amount}:{effect.Card}:{effect.Affects}"));
            string upgrades = string.Join(", ", context.World.Cards
                .Where(candidate => context.Cards.Kind(candidate.FaceId) == CardKind.Upgrade
                    && DeckTypes.IsInPlay(candidate.Area.Type))
                .Select(candidate => $"{candidate.FaceId}:{candidate.FaceUp}:"
                    + $"{candidate.Area.Type}:p{candidate.Area.PlayArea.Player}:"
                    + $"h{candidate.Area.Host}:o{candidate.Owner}"));
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} modified "
                + $"{match.Groups["field"].Value}; was {actual}; effects: [{effects}]; "
                + $"upgrades: [{upgrades}]");
        }
    }

    internal static void PlayerPlayAreaRemoved(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.GameAreaOf(PlayArea.Of(seat)) is not null
            || context.World.Cards.Any(card => card.Area.PlayArea == PlayArea.Of(seat)))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: seat {seat + 1}'s play area still exists");
        }
    }

    internal static void CardRemainingHitPoints(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        long actual = Math.Max(
            0,
            DamagePlacement.Health(context.World, context.Cards, card) - card.Damage);
        Equal(Number(match, "count", step), checked((int)actual),
            "remaining hit points", step);
    }

    internal static void CardCounters(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string type = match.Groups["type"].Value;
        string key = type == "threat" ? "k_threat" : $"c_{type}";
        Equal(
            Number(match, "count", step),
            checked((int)context.SceneRequired(step).Find(SceneCard(match, step))
                .Tokens.GetValueOrDefault(key)),
            $"{type} counters on the card",
            step);
    }

    internal static void CardTargetAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        if (context.LastCardOptions is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no target query has been made");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = match.Groups["availability"].Value == "available";
        bool actual = context.LastCardOptions.Contains(card.ObjectId);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be "
                + $"{match.Groups["availability"].Value} as a target");
        }
    }

    internal static void CardActionAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        if (context.LastCardOptions is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no card-action query has been made");
        }

        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool expected = match.Groups["availability"].Value == "available";
        bool actual = context.LastCardOptions.Contains(card.ObjectId);
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId}'s action to be "
                + match.Groups["availability"].Value);
        }
    }

    internal static void BasicRecoveryResult(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        bool expected = match.Groups["availability"].Value == "available";
        if (context.LastAvailability != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected basic recovery to be "
                + $"{match.Groups["availability"].Value}; was "
                + (context.LastAvailability is null
                    ? "not queried"
                    : context.LastAvailability.Value ? "available" : "unavailable"));
        }
    }

    internal static void CardPlayResult(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        BasicRecoveryResult(context, step, match);
    }

    internal static void ModifiedCardCost(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        long actual = CardPayment.CostOf(
            context.World,
            context.Cards,
            context.World.Seats[Seat(match, step)],
            card).Amount;
        Equal(
            Number(match, "count", step),
            checked((int)actual),
            "modified resource cost",
            step);
    }

    internal static void CardRemoved(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.RemovedArea)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} removed; was {card.Area}");
        }
    }

    internal static void CardIsVillain(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = context.World.TheCardIn(DeckType.VillainArea);
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} as villain; was "
                + (actual is null ? "<none>" : $"{actual.ObjectId}, faceup={actual.FaceUp}"));
        }
    }

    internal static void CardInVillainDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.VillainDeck)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in the villain deck; "
                + $"was {card.Area}");
        }
    }

    internal static void CardIsMainScheme(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card expected = context.SceneRequired(step).Find(SceneCard(match, step));
        Card? actual = context.World.TheCardIn(DeckType.MainSchemesArea);
        if (!ReferenceEquals(actual, expected) || !expected.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected faceup card {expected.ObjectId} "
                + $"({string.Join(',', expected.Faces)}) as main scheme; was "
                + (actual is null
                    ? "<none>"
                    : $"{actual.ObjectId} ({string.Join(',', actual.Faces)}), "
                        + $"face={actual.FaceId}, faceup={actual.FaceUp}"));
        }
    }

    internal static void CardInEncounterDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.EncounterDeck)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in the encounter deck; "
                + $"was {card.Area}");
        }
    }

    internal static void CardInSeatNemesis(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Nemesis))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in seat {seat + 1}'s "
                + $"set-aside nemesis pile; was {card.Area}");
        }
    }

    internal static void CardInPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} in play; was {card.Area.Type}");
        }
    }

    internal static void CardOutOfPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} out of play; was {card.Area.Type}");
        }
    }

    internal static void CardInPlayerPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Of(Seat(match, step)), expected: true);

    internal static void CardNotInPlayerPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Of(Seat(match, step)), expected: false);

    internal static void CardInVillainPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Villains, expected: true);

    internal static void CardNotInVillainPlayArea(
        TranscriptContext context, TranscriptStep step, Match match) =>
        CardPlayArea(context, step, match, PlayArea.Villains, expected: false);

    internal static void CardPlayArea(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        PlayArea playArea,
        bool expected)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        bool actual = card.Area.PlayArea == playArea;
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} "
                + (expected ? $"in play area {playArea}" : $"outside play area {playArea}")
                + $"; was {card.Area.PlayArea}");
        }
    }

    internal static void PlayerOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int[] expected = [.. match.Groups["order"].Value
            .Split(',')
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture) - 1)];
        if (!context.World.PlayerOrder.SequenceEqual(expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected player order "
                + $"{string.Join(',', expected.Select(seat => seat + 1))}; was "
                + string.Join(',', context.World.PlayerOrder.Select(seat => seat + 1)));
        }
    }

    internal static void PerPlayerCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step), context.World.Players,
            "players counted by the per-player icon", step);

}
