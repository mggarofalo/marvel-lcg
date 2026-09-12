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

internal static class CoreTranscriptCardAssertions
{
    internal static void CardEventOrder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int card = context.SceneRequired(step).Find(SceneCard(match, step)).ObjectId;
        int first = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["first"].Value
            && EventLands(gameEvent, card));
        int second = context.Events.FindIndex(gameEvent =>
            gameEvent.Verb == match.Groups["second"].Value
            && EventLands(gameEvent, card));
        if (first < 0 || second <= first)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {match.Groups["first"].Value} before "
                + $"{match.Groups["second"].Value} for card {card}");
        }
    }

    internal static void CardDiscardAfterEvent(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int card = context.SceneRequired(step).Find(SceneCard(match, step)).ObjectId;
        // Reveal keeps a treachery's event verb while moving it from the
        // resolving area to the encounter discard pile, so use the card's
        // final landing rather than the engine's operation spelling.
        int discard = context.Events.FindLastIndex(gameEvent => EventLands(gameEvent, card));
        int prior = context.Events.FindLastIndex(gameEvent =>
            gameEvent.Verb == match.Groups["verb"].Value);
        if (prior < 0 || discard <= prior)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card} discarded after "
                + $"{match.Groups["verb"].Value}");
        }
    }

    internal static bool EventLands(GameEvent gameEvent, int card) =>
        gameEvent is CardsMoved moved
        && moved.Cards.Any(landing => landing.Card == card);

    internal static void SeatForm(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        string expected = match.Groups["form"].Value == "alter-ego"
            ? Forms.AlterEgo
            : Forms.Hero;
        if (!Forms.In(context.World, context.World.Seats[seat], context.Cards, expected))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} in {expected} form");
        }
    }

    internal static void IdentityFaceOutOfPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card identity = context.World.Seats[Seat(match, step)].IdentityCard;
        string face = match.Groups["face"].Value;
        if (!identity.Faces.Contains(face, StringComparer.Ordinal)
            || string.Equals(identity.FaceId, face, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: identity face {face} is not its out-of-play side");
        }
    }

    internal static void FormTransition(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string expectedFrom = match.Groups["from"].Value;
        string expectedTo = match.Groups["to"].Value;
        var expected = (Seat(match, step), expectedFrom, expectedTo);
        if (context.LastFormChange != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected form transition {expected}; was "
                + (context.LastFormChange?.ToString() ?? "<none>"));
        }
    }

    internal static string FormName(IReadOnlySet<string> forms) => forms.Single() switch
    {
        Forms.AlterEgo => "alter-ego",
        Forms.Hero => "hero",
        string form => form,
    };

    internal static void CardStatus(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!Statuses.Has(context.World, card, match.Groups["status"].Value))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to have "
                + $"{match.Groups["status"].Value} status");
        }
    }

    internal static void CardOwned(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Equal(Seat(match, step), card.Owner, "card owner", step);
    }

    internal static void CardControlled(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!DeckTypes.IsInPlay(card.Area.Type)
            || !card.Area.PlayArea.IsPlayers)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} is not a controlled in-play card");
        }

        Equal(Seat(match, step), card.Area.PlayArea.Player, "card controller", step);
    }

    internal static void UpgradeAttachedIdentity(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Card identity = context.World.Seats[seat].IdentityCard;
        if (card.Area.Type != DeckType.UpgradesArea
            || card.Area.Host != identity.ObjectId
            || card.Area.PlayArea != identity.Area.PlayArea)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} attached to "
                + $"seat {seat + 1}'s identity; was {card.Area}");
        }
    }

    internal static void CardAttachedToCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CanonicalCoreScene scene = context.SceneRequired(step);
        Card card = scene.Find(SceneCard(match, step));
        Card host = scene.Find(new SceneCard(
            match.Groups["host"].Value,
            Number(match, "hostCopy", step)));
        if (card.Area.Type != DeckType.UpgradesArea
            || card.Area.Host != host.ObjectId)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} attached to "
                + $"card {host.ObjectId}; was {card.Area}");
        }
    }

    internal static void FacedownCardAttachedToCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CardAttachedToCard(context, step, match);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.FaceUp)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected attached card {card.ObjectId} facedown");
        }
    }

    internal static void StatusCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        Equal(
            Number(match, "count", step),
            Statuses.Count(context.World, card, match.Groups["status"].Value),
            $"{match.Groups["status"].Value} status cards",
            step);
    }

    internal static void CardAfflicted(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string status = match.Groups["status"].Value;
        if (!Statuses.Afflicted(context.World, context.Cards, card, status))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to be {status}");
        }
    }

    internal static void CardTrait(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string trait = match.Groups["trait"].Value.Replace(' ', '_');
        if (!Traits.Has(context.World, card, trait, context.Cards))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} to have trait {trait}");
        }
    }

    internal static void CardDoesNotHaveTrait(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        string trait = match.Groups["trait"].Value.Replace(' ', '_');
        if (Traits.Has(context.World, card, trait, context.Cards))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} not to have trait {trait}");
        }
    }

    internal static void PrintedCharacteristics(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string face = match.Groups["face"].Value;
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!string.Equals(context.LastInspectedFace, face, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: printed characteristics for {face} were not requested");
        }
        TranscriptTable table = Table(step, "field", "value");
        foreach (IReadOnlyDictionary<string, string> row in table.Rows)
        {
            string field = row["field"];
            string actual = field switch
            {
                "name" => context.Cards.Title(face),
                "subtitle" => context.Cards.Subtitle(face),
                "type" => PrintedType(context.Cards.Kind(face)),
                "traits" => string.Join('/', context.Cards.Traits(face)
                    .Select(trait => trait.Replace('_', ' '))),
                _ when field.StartsWith("attribute:", StringComparison.Ordinal) =>
                    context.Cards.Attributes(face).TryGetValue(
                        field["attribute:".Length..], out string? value)
                        ? value
                        : "<absent>",
                _ => throw new TranscriptException(
                    $"{step.Location}: unknown printed field '{field}'"),
            };
            if (!string.Equals(row["value"], actual, StringComparison.Ordinal))
            {
                throw new TranscriptAssertionException(
                    $"{step.Location}: expected {face} {field} '{row["value"]}'; was '{actual}'");
            }
        }
    }

    internal static void PendingCardOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending action prompt");
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (!asked.Affordances.Any(offer => offer.AnchorId == card.ObjectId))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} is not offered by '{asked.Label}'; "
                + $"offered [{string.Join(',', asked.Affordances.Select(offer => offer.AnchorId))}]");
        }
    }

    internal static void PendingCardNotOffered(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Prompt asked = context.PendingPrompt
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending setup prompt");
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (asked.Affordances.Any(offer => offer.AnchorId == card.ObjectId))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: card {card.ObjectId} was offered by '{asked.Label}'");
        }
    }

    internal static string PrintedType(CardKind kind) => kind switch
    {
        CardKind.EncounterVillain => "Villain",
        CardKind.EncounterSideScheme => "SideScheme",
        _ => kind.ToString(),
    };

    internal static void Equal(int expected, int actual, string observation, TranscriptStep step)
    {
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} {observation}; was {actual}");
        }
    }

    internal static int Seat(Match match, TranscriptStep step)
    {
        int oneBased = Number(match, "seat", step);
        if (oneBased <= 0)
        {
            throw new TranscriptException($"{step.Location}: seat numbers begin at 1");
        }

        return oneBased - 1;
    }

    internal static int Number(Match match, string group, TranscriptStep step)
    {
        if (!int.TryParse(
                match.Groups[group].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int value))
        {
            throw new TranscriptException(
                $"{step.Location}: '{match.Groups[group].Value}' is not a {group}");
        }

        return value;
    }

    internal static SceneCard SceneCard(Match match, TranscriptStep step) => new(
        match.Groups["face"].Value,
        Number(match, "copy", step));

    internal static int TableNumber(
        IReadOnlyDictionary<string, string> row, string column, TranscriptStep step)
    {
        if (!int.TryParse(
                row[column], NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            throw new TranscriptException(
                $"{step.Location}: '{row[column]}' is not a non-negative {column}");
        }

        return value;
    }

    internal static IReadOnlyDictionary<string, string> OneRow(
        TranscriptStep step, params string[] columns)
    {
        TranscriptTable table = Table(step, columns);
        if (table.Rows.Count != 1)
        {
            throw new TranscriptException(
                $"{step.Location}: expected exactly one table row; found {table.Rows.Count}");
        }

        return table.Rows[0];
    }

    internal static TranscriptTable Table(TranscriptStep step, params string[] columns)
    {
        TranscriptTable table = step.Table
            ?? throw new TranscriptException($"{step.Location}: step requires a table");
        var unused = table.Header.Except(columns, StringComparer.Ordinal).ToList();
        var missing = columns.Except(table.Header, StringComparer.Ordinal).ToList();
        if (unused.Count > 0 || missing.Count > 0)
        {
            string detail = string.Join("; ", new[]
            {
                unused.Count == 0 ? null : $"unused columns: {string.Join(", ", unused)}",
                missing.Count == 0 ? null : $"missing columns: {string.Join(", ", missing)}",
            }.Where(value => value is not null));
            throw new TranscriptException($"{step.Location}: {detail}");
        }

        return table;
    }

    internal static TranscriptException Failure(
        TranscriptContext context,
        TranscriptScenario scenario,
        TranscriptStep step,
        TranscriptFailureKind kind,
        string reason,
        Exception? inner)
    {
        string digest = context.Scene is null
            ? "<scene not constructed>"
            : context.World.Digest().Fingerprint();
        string recent = context.Events.Count == 0
            ? "<none>"
            : string.Join(Environment.NewLine,
                context.Events.TakeLast(12).Select(gameEvent =>
                    $"  - {gameEvent.GetType().Name}: {gameEvent}"));
        string message = $"""
            obligation: {scenario.Obligation}
            feature: {scenario.Location.Path}
            line: {step.Location.Line}
            step: {step.Kind} {step.Text}
            reason: {reason}
            world-digest: {digest}
            current-prompt: {context.CurrentPrompt}
            recent-events:
            {recent}
            """;
        return new TranscriptException(kind, message, inner, digest);
    }
}
