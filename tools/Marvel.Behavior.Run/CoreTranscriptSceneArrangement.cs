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

internal static class CoreTranscriptSceneArrangement
{
    internal static void DealScene(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        TranscriptTable table = step.Table
            ?? throw new TranscriptException($"{step.Location}: expected a table");
        ValidateSceneTable(table, step);
        context.Scene = CanonicalCoreScene.Deal(
            SceneRequest(context, table, step), context.Setup, context.Cards,
            new AbilityRunner(context.Abilities));
    }

    internal static void ValidateSceneTable(
        TranscriptTable table, TranscriptStep step)
    {
        string[] required = ["campaign", "heroes", "seed"];
        string[] allowed = [.. required, "modular sets", "decks"];
        var unused = table.Header.Except(allowed, StringComparer.Ordinal).ToList();
        var missing = required.Except(table.Header, StringComparer.Ordinal).ToList();
        if (unused.Count > 0 || missing.Count > 0)
        {
            string detail = string.Join("; ", new[]
            {
                unused.Count == 0 ? null : $"unused columns: {string.Join(", ", unused)}",
                missing.Count == 0 ? null : $"missing columns: {string.Join(", ", missing)}",
            }.Where(value => value is not null));
            throw new TranscriptException(
                $"{step.Location}: {detail}");
        }
        if (table.Rows.Count != 1)
            throw new TranscriptException(
                $"{step.Location}: expected exactly one table row; found {table.Rows.Count}");
    }

    internal static CoreSceneRequest SceneRequest(
        TranscriptContext context, TranscriptTable table, TranscriptStep step)
    {
        IReadOnlyDictionary<string, string> row = table.Rows[0];
        if (!uint.TryParse(row["seed"], NumberStyles.None, CultureInfo.InvariantCulture,
                out uint seed))
            throw new TranscriptException($"{step.Location}: seed must be an unsigned integer");
        var heroes = Values(row["heroes"]);
        if (heroes.Count == 0)
            throw new TranscriptException($"{step.Location}: heroes must not be empty");
        return new CoreSceneRequest(
            context.Obligation, row["campaign"], heroes, seed,
            row.TryGetValue("modular sets", out string? modular)
                ? Values(modular) : null,
            PlayerDecks: row.TryGetValue("decks", out string? decks)
                ? Values(decks) : null);
    }

    internal static IReadOnlyList<string> Values(string text) =>
        [.. text.Split(',', StringSplitOptions.TrimEntries
            | StringSplitOptions.RemoveEmptyEntries)];

    internal static void StackPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Discard);

    internal static void StackPlayerDeckWithEmptyDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Hand);

    internal static void StackPlayerDeckLeavingRemainder(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Leave);

    internal static void StackPlayerDeck(
        TranscriptContext context,
        TranscriptStep step,
        Match match,
        PlayerDeckRemainder remainder)
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "next card", "copy");
        context.SceneRequired(step).Apply(new StackPlayerDeck(
            seat,
            [.. table.Rows.Select(row => new SceneCard(
                row["next card"], TableNumber(row, "copy", step)))],
            remainder));
    }

    internal static void SetPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        context.SceneRequired(step).Apply(new SetPlayerHand(
            Seat(match, step),
            [.. table.Rows.Select(row => new SceneCard(
                row["card"], TableNumber(row, "copy", step)))]));
    }

    internal static void SetEmptyPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetPlayerHand(Seat(match, step), []));

    internal static void SetCardReadiness(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneReady(
            SceneCard(match, step),
            string.Equals(match.Groups["state"].Value, "ready", StringComparison.Ordinal)));

    internal static void SetCardDamage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneDamage(
            SceneCard(match, step), Number(match, "count", step)));

    internal static void SetCardCounters(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneCounters(
            SceneCard(match, step),
            match.Groups["type"].Value,
            Number(match, "count", step)));

    internal static void SetAccelerationTokens(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneAccelerationTokens(
            Number(match, "count", step)));

    internal static void SetIdentityFace(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneForm(
            Seat(match, step), match.Groups["face"].Value));

    internal static void SetVillainStage(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneVillain(SceneCard(match, step)));

    internal static void PlaceAlly(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Ally, Seat(match, step))));

    internal static void PlaceSupport(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Support, Seat(match, step))));

    internal static void EngageMinion(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.EngagedMinion, Seat(match, step))));

    internal static void EngageFacedownDrone(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        SceneCard card = SceneCard(match, step);
        context.SceneRequired(step).Apply(new StackPlayerDeck(
            seat, [card], PlayerDeckRemainder.Leave));
        Card? engaged = FacedownDrones.EngageTop(
            context.World, seat, "behavioral fixture", "Create_Drone", context.Events);
        if (engaged != context.SceneRequired(step).Find(card))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.FaceId} copy {card.Copy} was not the created Drone");
        }
    }

    internal static void ClearFacedownDrones(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        foreach (Card drone in FacedownDrones.EngagedWith(context.World, seat))
        {
            drone.TurnFaceUp();
            World.MoveToTop(drone, context.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(drone.Owner), cardOwner: drone.Owner));
        }
    }

    internal static void PlaceSideScheme(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step), new SceneDestination(SceneZone.SideScheme)));

    internal static void PlaceObligation(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Obligation, Seat(match, step))));

    internal static void PlacePlayerDiscard(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.PlayerDiscard, Seat(match, step))));

    internal static void AttachIdentityUpgrade(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Upgrade, Seat(match, step))));

    internal static void AttachCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        CanonicalCoreScene scene = context.SceneRequired(step);
        Card card = scene.Find(SceneCard(match, step));
        var host = scene.Find(new SceneCard(
            match.Groups["host"].Value,
            Number(match, "hostCopy", step)));
        SceneDestination destination = card.Owner == World.Scenario
            ? new SceneDestination(SceneZone.Attachment, Host: host.ObjectId)
            : new SceneDestination(SceneZone.Upgrade, card.Owner, host.ObjectId);
        scene.Apply(new MoveSceneCard(
            SceneCard(match, step),
            destination));
    }

    internal static void GiveCardStatus(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new GiveSceneStatus(
            SceneCard(match, step), match.Groups["status"].Value));

    internal static void StackEncounterDeckWithDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        StackEncounterDeck(
            context, step, EncounterDeckRemainder.Discard, seat: 0);
    }

    internal static void StackEncounterDeckWithDealtCards(
        TranscriptContext context, TranscriptStep step, Match match) =>
        StackEncounterDeck(
            context,
            step,
            EncounterDeckRemainder.Dealt,
            Seat(match, step));

    internal static void StackEncounterDeckLeavingRemainder(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        StackEncounterDeck(
            context, step, EncounterDeckRemainder.Leave, seat: 0);
    }

    internal static void StackEncounterDeck(
        TranscriptContext context,
        TranscriptStep step,
        EncounterDeckRemainder remainder,
        int seat)
    {
        TranscriptTable table = Table(step, "next card", "copy");
        context.SceneRequired(step).Apply(new StackEncounterDeck(
            [.. table.Rows.Select(row => new SceneCard(
                row["next card"], TableNumber(row, "copy", step)))],
            remainder,
            seat));
    }

}
