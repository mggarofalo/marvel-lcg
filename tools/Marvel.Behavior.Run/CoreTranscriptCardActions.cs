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

internal static class CoreTranscriptCardActions
{
    internal static void MinionEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        int seat = Seat(match, step);
        Card minion = context.SceneRequired(step).Find(SceneCard(match, step));
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.EngagedMinion, seat)));
        Reveal.Quickstrike(context.World, context.Cards, minion, seat, round: 1);
        FinishAgenda(context, step);
    }

    internal static void SupportEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Support, Seat(match, step))));
    }

    internal static void UpgradeEntersPlay(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.SceneRequired(step).Apply(new MoveSceneCard(
            SceneCard(match, step),
            new SceneDestination(SceneZone.Upgrade, Seat(match, step))));
    }

    internal static void RequestPrintedCharacteristics(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = context.SceneRequired(step).Find(SceneCard(match, step));
        context.LastInspectedFace = match.Groups["face"].Value;
    }

    internal static void InitiateActionWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    internal static void InitiateActionWithoutPayment(
        TranscriptContext context, TranscriptStep step, Match match) =>
        InitiateAction(context, step, match, []);

    internal static void InitiateActionWithDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [],
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    internal static void InitiateIndexedActionWithVariable(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        InitiateAction(
            context,
            step,
            match,
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(new SceneCard(
                row["card"], TableNumber(row, "copy", step))).ObjectId)],
            ordinal: PrintedOrdinal(match, step),
            values: new Dictionary<string, long>(StringComparer.Ordinal)
            {
                ["X"] = Number(match, "value", step),
            });
    }

    internal static void InitiateIndexedActionWithoutPayment(
        TranscriptContext context, TranscriptStep step, Match match) =>
        InitiateAction(
            context, step, match, [], ordinal: PrintedOrdinal(match, step));

    internal static int PrintedOrdinal(Match match, TranscriptStep step) =>
        match.Groups["ordinal"].Value switch
        {
            "first" => 0,
            "second" => 1,
            _ => throw new TranscriptException(
                $"{step.Location}: unknown printed action ordinal '{match.Groups["ordinal"].Value}'"),
        };

    internal static void PlayCardWithPayment(
        TranscriptContext context, TranscriptStep step, Match match) =>
        PlayCardWithPayment(context, step, match, explicitTarget: null);

    internal static void PlayCardWithTargetAndPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int target = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["target"].Value,
            Number(match, "targetCopy", step))).ObjectId;
        PlayCardWithPayment(context, step, match, target);
    }

    internal static void PlayCardWithPayment(
        TranscriptContext context, TranscriptStep step, Match match, int? explicitTarget)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        TranscriptTable table = Table(step, "card", "copy");
        int[] payment = [.. table.Rows.Select(row => context.SceneRequired(step).Find(
            new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)];
        var abilityResources = context.World.Abilities
            .ResourceAbilities(context.World, seat)
            .Where(source => payment.Contains(source.Effect))
            .ToList();
        context.LastResourceGeneration = abilityResources.Count == 1
            ? (abilityResources[0].Effect, abilityResources[0].Generates)
            : null;

        // Some card transcripts need to fix the player deck before playing a
        // card. Setup's opening-hand draw would consume that authority-derived
        // stack, so the runner may exercise the ordinary CardPlay entry point
        // directly from the otherwise-complete canonical scene.
        if (context.Game is null)
        {
            context.Events.Clear();
            CardPlay.Play(
                context.World,
                context.Cards,
                context.World.Abilities,
                context.World.Seats[seat],
                card,
                payment,
                context.Events,
                explicitTarget is { } target ? [target] : []);
            SetPendingPrompt(context, Sequence.Work(
                context.World, context.Cards, context.World.Abilities, context.Events));
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

        Affordance play = asked.Affordances.Single(candidate =>
            candidate.Verb == CardPlay.Verb && candidate.AnchorId == card.ObjectId);
        int[] targets = (play.Targets, explicitTarget) switch
        {
            (null, null) => [],
            ({ } request, { } target) when request.Legal.Contains(target) => [target],
            ({ } request, { } target) => throw new TranscriptException(
                $"{step.Location}: card {target} is not a legal target for playing "
                + $"card {card.ObjectId}; legal targets are {string.Join(",", request.Legal)}"),
            ({ Legal.Count: 1 } request, null) => [request.Legal[0]],
            _ => throw new TranscriptException(
                $"{step.Location}: playing card {card.ObjectId} requires an explicit target"),
        };
        context.Events.Clear();
        Resolution resolution = game.Resolve(Decision.Take(play.Id, targets, payment));
        context.Events.AddRange(resolution.Events);
        SetPendingPrompt(context, resolution.Prompt);
    }

    internal static void RequestCardActions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        context.LastCardOptions = ((AbilityRunner)context.World.Abilities)
            .Actions(context.World, seat)
            .Select(action => action.Card)
            .ToHashSet();
    }

    internal static void UseCardResourceAbility(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        var runner = (AbilityRunner)context.World.Abilities;
        if (!runner.ResourceAbilities(context.World, seat)
            .Any(candidate => candidate.Effect == card.ObjectId))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} has no available resource ability");
        }

        context.Events.Clear();
        string resources = runner.UseResource(
            context.World, seat, card.ObjectId, context.Events);
        context.LastResourceGeneration = (card.ObjectId, resources);
    }

    internal static void RequestTurnCardActions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Game game = context.Game
            ?? throw new TranscriptException($"{step.Location}: game setup has not begun");
        int seat = Seat(match, step);
        Prompt asked = game.Pending
            ?? throw new TranscriptException($"{step.Location}: no turn prompt is pending");
        if (game.Phase != GamePhase.PlayerTurn || game.Active != seat || asked.Player != seat)
        {
            throw new TranscriptException(
                $"{step.Location}: seat {seat + 1} is not taking their turn");
        }

        context.LastCardOptions = asked.Affordances
            .Where(option => option.Verb is not Game.ChangeForm and not Game.EndPhaseVerb
                && option.Verb != CardPlay.Verb)
            .Select(option => option.AnchorId)
            .ToHashSet();
    }

}
