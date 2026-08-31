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
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

internal sealed record TranscriptResult(
    string Obligation,
    string Scenario,
    string Digest,
    IReadOnlyList<GameEvent> Events);

internal sealed record TranscriptBinding(
    string Name,
    TranscriptStepKind Kind,
    Regex Pattern,
    Action<TranscriptContext, TranscriptStep, Match> Execute);

internal sealed record BoundTranscriptStep(
    TranscriptStep Step,
    TranscriptBinding Binding,
    Match Match);

internal sealed class TranscriptContext
{
    public TranscriptContext(
        string obligation,
        SetupCatalog setup,
        CardCatalog cards,
        AbilityBook abilities)
    {
        Obligation = obligation;
        Setup = setup;
        Cards = cards;
        Abilities = abilities;
    }

    public string Obligation { get; }

    public SetupCatalog Setup { get; }

    public CardCatalog Cards { get; }

    public AbilityBook Abilities { get; }

    public CanonicalCoreScene? Scene { get; set; }

    public List<GameEvent> Events { get; } = [];

    public string CurrentPrompt { get; set; } = "<none>";

    public string? ExpectedException { get; set; }

    public RulesNotImplementedException? PendingException { get; set; }

    public string? ExceptionDigest { get; set; }

    public bool ExceptionObserved { get; set; }

    public World World => Scene?.World
        ?? throw new TranscriptException("a canonical Core scene has not been constructed");
}

internal sealed class CoreTranscriptRunner
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);
    private readonly IReadOnlyList<TranscriptBinding> bindings;
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityBook abilities;

    public CoreTranscriptRunner(string root)
        : this(root, null)
    {
    }

    internal CoreTranscriptRunner(
        string root, IReadOnlyList<TranscriptBinding>? bindingOverride)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        setup = SetupCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "setup", "setup.json")));
        cards = CardCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "cards", "cards.json")));
        abilities = AbilityCatalog.Parse(File.ReadAllText(
            Path.Combine(root, "datasets", "abilities", "abilities.json")));
        bindings = bindingOverride ?? DefaultVocabulary();
    }

    public TranscriptResult Execute(TranscriptScenario scenario) => Execute(scenario, null);

    internal TranscriptResult Execute(
        TranscriptScenario scenario, string? expectedException)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var context = new TranscriptContext(scenario.Obligation, setup, cards, abilities)
        {
            ExpectedException = expectedException,
        };
        IReadOnlyList<BoundTranscriptStep> bound = BindAll(context, scenario);
        foreach (BoundTranscriptStep current in bound)
        {
            TranscriptStep step = current.Step;
            if (context.ExceptionObserved)
            {
                throw Failure(context, scenario, step, TranscriptFailureKind.Validation,
                    "unused step after the expected exception was observed", null);
            }

            if (context.PendingException is not null
                && current.Binding.Name != "cataloged-exception")
            {
                throw Failure(context, scenario, step, TranscriptFailureKind.Validation,
                    "the step after an unimplemented decision must observe its cataloged exception",
                    null);
            }

            try
            {
                current.Binding.Execute(context, step, current.Match);
            }
            catch (RulesNotImplementedException error)
                when (step.Kind == TranscriptStepKind.When
                    && context.ExpectedException is not null)
            {
                if (context.Scene is null)
                {
                    throw Failure(context, scenario, step,
                        TranscriptFailureKind.Execution,
                        "unimplemented decision was reached before a legal scene existed",
                        error);
                }

                context.PendingException = error;
                context.ExceptionDigest = context.World.Digest().Fingerprint();
            }
            catch (TranscriptAssertionException error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Assertion, error.Message, error);
            }
            catch (TranscriptException error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Execution, error.Message, error);
            }
            catch (Exception error)
            {
                throw Failure(context, scenario, step,
                    TranscriptFailureKind.Execution,
                    $"{error.GetType().Name}: {error.Message}", error);
            }
        }

        if (context.Scene is null)
        {
            throw new TranscriptException(
                $"{scenario.Location}: {scenario.Obligation}: scenario never constructs a scene");
        }

        if (context.ExpectedException is not null && !context.ExceptionObserved)
        {
            throw new TranscriptException(
                $"{scenario.Location}: expected '{context.ExpectedException}', but the scenario completed");
        }

        return new TranscriptResult(
            scenario.Obligation,
            $"{scenario.Location.Path}::{scenario.Name}",
            context.ExceptionDigest ?? context.World.Digest().Fingerprint(),
            [.. context.Events]);
    }

    private List<BoundTranscriptStep> BindAll(
        TranscriptContext context, TranscriptScenario scenario)
    {
        var bound = new List<BoundTranscriptStep>();
        foreach (TranscriptStep step in scenario.Steps)
        {
            var matches = bindings
                .Where(binding => binding.Kind == step.Kind)
                .Select(binding => (Binding: binding, Match: binding.Pattern.Match(step.Text)))
                .Where(candidate => candidate.Match.Success)
                .ToList();
            if (matches.Count != 1)
            {
                string reason = matches.Count == 0
                    ? $"unknown {step.Kind} step '{step.Text}'"
                    : $"ambiguous {step.Kind} step '{step.Text}'; matched "
                      + string.Join(", ", matches.Select(candidate => candidate.Binding.Name));
                TranscriptFailureKind kind = matches.Count == 0
                    ? TranscriptFailureKind.UnknownStep
                    : TranscriptFailureKind.AmbiguousStep;
                throw Failure(context, scenario, step, kind, reason, null);
            }

            bound.Add(new BoundTranscriptStep(step, matches[0].Binding, matches[0].Match));
        }

        return bound;
    }

    internal static IReadOnlyList<TranscriptBinding> DefaultVocabulary() =>
    [
        Bind("core-scene", TranscriptStepKind.Given,
            "a canonical Core scene is dealt", DealScene),
        Bind("stack-player-deck", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards", StackPlayerDeck),
        Bind("stack-player-deck-empty-discard", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards with all other deck cards in hand",
            StackPlayerDeckWithEmptyDiscard),
        Bind("set-player-hand", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s hand contains exactly these cards", SetPlayerHand),
        Bind("set-card-readiness", TranscriptStepKind.Given,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            SetCardReadiness),
        Bind("stack-encounter-deck-discard", TranscriptStepKind.Given,
            "the encounter deck contains only these next cards with all other deck cards in the encounter discard pile",
            StackEncounterDeckWithDiscard),
        Bind("stack-encounter-deck-dealt", TranscriptStepKind.Given,
            @"the encounter deck contains only these next cards with all other deck cards dealt facedown to seat (?<seat>\d+)",
            StackEncounterDeckWithDealtCards),
        Bind("draw-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) draws (?<count>\d+) cards?", DrawCards),
        Bind("discard-player-deck", TranscriptStepKind.When,
            @"seat (?<seat>\d+) discards the top (?<count>\d+) cards? of their player deck",
            DiscardPlayerDeck),
        Bind("discard-from-hand", TranscriptStepKind.When,
            @"seat (?<seat>\d+) discards card (?<face>\d+[a-z]?) copy (?<copy>\d+) from hand",
            DiscardFromHand),
        Bind("deal-encounter-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) is dealt (?<count>\d+) encounter cards?",
            DealEncounterCards),
        Bind("discard-encounter-deck", TranscriptStepKind.When,
            @"the top (?<count>\d+) cards? of the encounter deck (?:is|are) discarded",
            DiscardEncounterDeck),
        Bind("phase-discard-none", TranscriptStepKind.When,
            @"seat (?<seat>\d+) keeps every card during the optional end-of-player-phase discard",
            PhaseDiscardNone),
        Bind("phase-discard-selected", TranscriptStepKind.When,
            @"seat (?<seat>\d+) chooses these cards for the end-of-player-phase discard",
            PhaseDiscardSelected),
        Bind("phase-draw", TranscriptStepKind.When,
            "the end-of-player-phase draw step resolves", PhaseDraw),
        Bind("phase-ready", TranscriptStepKind.When,
            "the end-of-player-phase ready step resolves", PhaseReady),
        Bind("hand-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in hand", HandCount),
        Bind("player-deck-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their player deck", PlayerDeckCount),
        Bind("player-discard-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their discard pile", PlayerDiscardCount),
        Bind("encounter-queue-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) facedown encounter cards?", EncounterCount),
        Bind("encounter-deck-count", TranscriptStepKind.Then,
            @"the encounter deck has (?<count>\d+) cards?", EncounterDeckCount),
        Bind("encounter-discard-count", TranscriptStepKind.Then,
            @"the encounter discard pile has (?<count>\d+) cards?", EncounterDiscardCount),
        Bind("acceleration-token-count", TranscriptStepKind.Then,
            @"the main scheme has (?<count>\d+) acceleration tokens?", AccelerationTokenCount),
        Bind("card-readiness", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is (?<state>ready|exhausted)",
            CardReadiness),
        Bind("card-player-discard", TranscriptStepKind.Then,
            @"card (?<face>\d+[a-z]?) copy (?<copy>\d+) is in seat (?<seat>\d+)'s discard pile",
            CardInPlayerDiscard),
        Bind("not-eliminated", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is not eliminated", NotEliminated),
        Bind("game-unfinished", TranscriptStepKind.Then,
            "the game is unfinished", GameUnfinished),
        Bind("event-emitted", TranscriptStepKind.Then,
            @"a (?<verb>[A-Za-z_]+) event with trigger (?<trigger>.+) was emitted", EventEmitted),
        Bind("event-count", TranscriptStepKind.Then,
            @"(?<count>\d+) (?<verb>[A-Za-z_]+) events? (?:was|were) emitted", EventCount),
        Bind("event-order", TranscriptStepKind.Then,
            @"a (?<first>[A-Za-z_]+) event was emitted before a (?<second>[A-Za-z_]+) event",
            EventOrder),
        Bind("players-lose", TranscriptStepKind.Then,
            "the players lose the game", PlayersLose),
        Bind("cataloged-exception", TranscriptStepKind.Then,
            "the engine raises the cataloged unimplemented rule exception", CatalogedException),
    ];

    private static TranscriptBinding Bind(
        string name,
        TranscriptStepKind kind,
        string pattern,
        Action<TranscriptContext, TranscriptStep, Match> execute) =>
        new(name, kind, new Regex(
            $"\\A{pattern}\\z",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            PatternTimeout), execute);

    private static void DealScene(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        IReadOnlyDictionary<string, string> row = OneRow(
            step, "campaign", "heroes", "seed");
        if (!uint.TryParse(row["seed"], NumberStyles.None, CultureInfo.InvariantCulture,
                out uint seed))
        {
            throw new TranscriptException($"{step.Location}: seed must be an unsigned integer");
        }

        IReadOnlyList<string> heroes = [.. row["heroes"]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        if (heroes.Count == 0)
        {
            throw new TranscriptException($"{step.Location}: heroes must not be empty");
        }

        context.Scene = CanonicalCoreScene.Deal(
            new CoreSceneRequest(
                context.Obligation, row["campaign"], heroes, seed),
            context.Setup,
            context.Cards,
            new AbilityRunner(context.Abilities));
    }

    private static void StackPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Discard);

    private static void StackPlayerDeckWithEmptyDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
        => StackPlayerDeck(context, step, match, PlayerDeckRemainder.Hand);

    private static void StackPlayerDeck(
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

    private static void SetPlayerHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        context.SceneRequired(step).Apply(new SetPlayerHand(
            Seat(match, step),
            [.. table.Rows.Select(row => new SceneCard(
                row["card"], TableNumber(row, "copy", step)))]));
    }

    private static void SetCardReadiness(
        TranscriptContext context, TranscriptStep step, Match match) =>
        context.SceneRequired(step).Apply(new SetSceneReady(
            SceneCard(match, step),
            string.Equals(match.Groups["state"].Value, "ready", StringComparison.Ordinal)));

    private static void StackEncounterDeckWithDiscard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        StackEncounterDeck(
            context, step, EncounterDeckRemainder.Discard, seat: 0);
    }

    private static void StackEncounterDeckWithDealtCards(
        TranscriptContext context, TranscriptStep step, Match match) =>
        StackEncounterDeck(
            context,
            step,
            EncounterDeckRemainder.Dealt,
            Seat(match, step));

    private static void StackEncounterDeck(
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

    private static void DrawCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Draw.Cards(context.World, seat, count, "behavioral transcript", context.Events);
    }

    private static void DiscardPlayerDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = PlayerDeck.DiscardTop(
            context.World, seat, count, "behavioral transcript", context.Events);
    }

    private static void DiscardFromHand(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        var reference = new SceneCard(
            match.Groups["face"].Value, Number(match, "copy", step));
        Card card = context.SceneRequired(step).Find(reference);
        if (!ReferenceEquals(card.Area, context.World.Seats[seat].Hand))
        {
            throw new TranscriptException(
                $"{step.Location}: card {card.ObjectId} is not in seat {seat + 1}'s hand");
        }

        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Discard.Card(context.World, card, "behavioral transcript", context.Events);
    }

    private static void DealEncounterCards(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        int count = Number(match, "count", step);
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        for (int index = 0; index < count; index++)
        {
            _ = Deal.EncounterCard(
                context.World, seat, "behavioral transcript", context.Events);
        }
    }

    private static void DiscardEncounterDeck(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = EncounterDeck.DiscardTop(
            context.World,
            Number(match, "count", step),
            "behavioral transcript",
            context.Events);
    }

    private static void PhaseDiscardNone(
        TranscriptContext context, TranscriptStep step, Match match) =>
        PhaseDiscard(context, step, Seat(match, step), []);

    private static void PhaseDiscardSelected(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        TranscriptTable table = Table(step, "card", "copy");
        PhaseDiscard(
            context,
            step,
            Seat(match, step),
            [.. table.Rows.Select(row => context.SceneRequired(step).Find(
                new SceneCard(row["card"], TableNumber(row, "copy", step))).ObjectId)]);
    }

    private static void PhaseDiscard(
        TranscriptContext context,
        TranscriptStep step,
        int seat,
        IReadOnlyList<int> cards)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DiscardToHandSize(
            context.World, context.Cards, seat, cards, context.Events);
    }

    private static void PhaseDraw(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.DrawToHandSize(context.World, context.Cards, context.Events);
    }

    private static void PhaseReady(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = step;
        _ = match;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        PhaseEnd.ReadyCards(context.World, context.Events);
    }

    private static void HandCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Hand.Cards.Count,
            "cards in hand", step);

    private static void PlayerDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.Seats[Seat(match, step)].Deck.Cards.Count,
            "cards in the player deck", step);

    private static void PlayerDiscardCount(
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

    private static void EncounterCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DealtEncounterCardsDeck,
                PlayArea.Of(Seat(match, step))).Cards.Count,
            "facedown encounter cards", step);

    private static void EncounterDeckCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDeck).Cards.Count,
            "cards in the encounter deck", step);

    private static void EncounterDiscardCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(DeckType.EncounterDiscardPile).Cards.Count,
            "cards in the encounter discard pile", step);

    private static void AccelerationTokenCount(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new TranscriptException($"{step.Location}: no main scheme is in play");
        long actual = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        Equal(Number(match, "count", step), checked((int)actual),
            "acceleration tokens on the main scheme", step);
    }

    private static void CardReadiness(
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

    private static void CardInPlayerDiscard(
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
                + $"discard pile; was {card.Area}");
        }
    }

    private static void NotEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} not to be eliminated");
        }
    }

    private static void GameUnfinished(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.Unfinished)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected an unfinished game; was {context.World.Result}");
        }
    }

    private static void CatalogedException(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.ExpectedException is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no cataloged exception is expected for this obligation");
        }

        string actual = context.PendingException is null
            ? "(none)"
            : $"{context.PendingException.GetType().Name}: {context.PendingException.Message}";
        if (!string.Equals(actual, context.ExpectedException, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected '{context.ExpectedException}'; reached '{actual}'");
        }

        context.ExceptionObserved = true;
    }

    private static void EventEmitted(
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

    private static void EventCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(
            Number(match, "count", step),
            context.Events.Count(gameEvent =>
                gameEvent.Verb == match.Groups["verb"].Value),
            $"{match.Groups["verb"].Value} events",
            step);

    private static void EventOrder(
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

    private static void PlayersLose(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.PlayersLose)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected PlayersLose; was {context.World.Result}");
        }
    }

    private static void Equal(int expected, int actual, string observation, TranscriptStep step)
    {
        if (actual != expected)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected {expected} {observation}; was {actual}");
        }
    }

    private static int Seat(Match match, TranscriptStep step)
    {
        int oneBased = Number(match, "seat", step);
        if (oneBased <= 0)
        {
            throw new TranscriptException($"{step.Location}: seat numbers begin at 1");
        }

        return oneBased - 1;
    }

    private static int Number(Match match, string group, TranscriptStep step)
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

    private static SceneCard SceneCard(Match match, TranscriptStep step) => new(
        match.Groups["face"].Value,
        Number(match, "copy", step));

    private static int TableNumber(
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

    private static IReadOnlyDictionary<string, string> OneRow(
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

    private static TranscriptTable Table(TranscriptStep step, params string[] columns)
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

    private static TranscriptException Failure(
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
                context.Events.TakeLast(5).Select(gameEvent =>
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

internal static class TranscriptContextExtensions
{
    public static CanonicalCoreScene SceneRequired(
        this TranscriptContext context, TranscriptStep step) =>
        context.Scene ?? throw new TranscriptException(
            $"{step.Location}: a canonical Core scene must be dealt first");
}

internal sealed class CoreTranscriptSuite
{
    private readonly string root;
    private readonly CatalogEvidence catalog;
    private readonly CoreTranscriptRunner runner;

    public CoreTranscriptSuite(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        runner = new CoreTranscriptRunner(this.root);
        catalog = ReadCatalog(Path.Combine(
            this.root, "specs", "behavior", "catalog.json"));
    }

    public IReadOnlyList<TranscriptResult> RunPassingCorpus()
    {
        string directory = Path.Combine(root, "specs", "behavior", "core");
        var paths = Directory.EnumerateFiles(directory, "*.feature")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        if (paths.Count == 0)
        {
            throw new TranscriptException("specs/behavior/core contains no executable features");
        }

        var results = new List<TranscriptResult>();
        var executed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
            TranscriptFeature feature = TranscriptParser.Parse(root, path);
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                CatalogObligation obligation = ValidateAuthority(
                    scenario, requireCompletionEvidence: true);
                string reference = Reference(scenario);
                if (!executed.Add(reference))
                {
                    throw new TranscriptException(
                        $"{scenario.Location}: duplicate executed scenario reference '{reference}'");
                }
                if (obligation.Implementation == "supported")
                {
                    results.Add(runner.Execute(scenario));
                }
                else
                {
                    results.Add(RunUnimplemented(scenario, obligation));
                }
            }
        }

        ValidateScenarioCompleteness(ExpectedScenarioReferences(), executed);

        return results;
    }

    public TranscriptException RunQuarantine()
    {
        string path = Path.Combine(root, "specs", "self-test", "quarantine.feature");
        TranscriptFeature feature = TranscriptParser.Parse(
            root, path, "the executable runner rejects a false hand count");
        try
        {
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                _ = ValidateAuthority(scenario, requireCompletionEvidence: false);
                _ = runner.Execute(scenario);
            }
        }
        catch (TranscriptException expected)
            when (expected.Kind == TranscriptFailureKind.Assertion)
        {
            return expected;
        }
        catch (TranscriptException wrongFailure)
        {
            throw new TranscriptException(
                TranscriptFailureKind.Validation,
                "quarantine did not reach its deliberately false assertion",
                wrongFailure);
        }

        throw new TranscriptException(
            "specs/self-test/quarantine.feature passed; its false assertion no longer proves the runner");
    }

    internal void ValidateForPassing(TranscriptScenario scenario) =>
        _ = ValidateAuthority(scenario, requireCompletionEvidence: true);

    private CatalogObligation ValidateAuthority(
        TranscriptScenario scenario, bool requireCompletionEvidence)
    {
        CatalogObligation obligation = ValidateNamedAuthority(
            scenario, scenario.Obligation, requireCompletionEvidence, "primary");
        foreach (string covered in scenario.CoveredObligations)
        {
            CatalogObligation secondary = ValidateNamedAuthority(
                scenario, covered, requireCompletionEvidence, "covered");
            if (!string.Equals(
                    secondary.Implementation, obligation.Implementation,
                    StringComparison.Ordinal))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: covered obligation '{covered}' is "
                    + $"{secondary.Implementation}, but the primary obligation is "
                    + $"{obligation.Implementation}");
            }
        }

        if (requireCompletionEvidence)
        {
            string reference = Reference(scenario);
            var declared = scenario.CoveredObligations
                .Prepend(scenario.Obligation)
                .ToHashSet(StringComparer.Ordinal);
            var linked = catalog.Obligations.Values
                .Where(candidate => candidate.Scenarios.Contains(
                    reference, StringComparer.Ordinal))
                .Select(candidate => candidate.Id)
                .ToHashSet(StringComparer.Ordinal);
            if (!declared.SetEquals(linked))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: catalog coverage differs from declared coverage; "
                    + $"declared [{string.Join(", ", declared.Order())}], linked "
                    + $"[{string.Join(", ", linked.Order())}]");
            }
        }

        return obligation;
    }

    private CatalogObligation ValidateNamedAuthority(
        TranscriptScenario scenario,
        string obligationId,
        bool requireCompletionEvidence,
        string role)
    {
        if (!catalog.Obligations.TryGetValue(
                obligationId, out CatalogObligation? obligation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: stale or missing {role} obligation '{obligationId}'");
        }

        if (scenario.Authorities.Count == 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: scenario has no direct authority tags");
        }

        var missing = scenario.Authorities
            .Where(authority => !catalog.Sources.ContainsKey(authority))
            .ToList();
        if (missing.Count > 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: missing direct authorities: {string.Join(", ", missing)}");
        }

        var outsideCore = scenario.Authorities
            .Where(authority => catalog.Sources.TryGetValue(
                authority, out CatalogSource? source)
                && source.Disposition == "outside-core")
            .ToList();
        if (outsideCore.Count > 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: outside-Core direct authorities: "
                + string.Join(", ", outsideCore));
        }

        if (!scenario.Authorities.Contains(obligation.Source, StringComparer.Ordinal))
        {
            throw new TranscriptException(
                $"{scenario.Location}: {role} obligation derives from '{obligation.Source}', "
                + "which is not a direct authority tag");
        }

        if (!string.Equals(obligation.Disposition, "executable", StringComparison.Ordinal)
            || obligation.Implementation is not ("supported" or "unimplemented"))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' is "
                + $"{obligation.Disposition}/{obligation.Implementation ?? "(none)"}, "
                + "not executable with a completed implementation status");
        }

        if (!requireCompletionEvidence)
        {
            return obligation;
        }

        string reference = Reference(scenario);
        if (!obligation.Scenarios.Contains(reference, StringComparer.Ordinal))
        {
            throw new TranscriptException(
                $"{scenario.Location}: catalog does not link scenario '{reference}'");
        }

        if (string.IsNullOrWhiteSpace(obligation.Mutation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' has no mutation evidence");
        }

        if (obligation.Implementation == "unimplemented"
            && string.IsNullOrWhiteSpace(obligation.Exception))
        {
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' names no expected exception");
        }

        return obligation;
    }

    private TranscriptResult RunUnimplemented(
        TranscriptScenario scenario, CatalogObligation obligation) =>
        runner.Execute(scenario, obligation.Exception);

    private IReadOnlySet<string> ExpectedScenarioReferences() =>
        CompletedScenarioReferences(catalog.Obligations.Values);

    internal static IReadOnlySet<string> CompletedScenarioReferences(
        IEnumerable<CatalogObligation> obligations)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (CatalogObligation obligation in obligations.Where(obligation =>
                     obligation.Disposition == "executable"
                     && obligation.Implementation is "supported" or "unimplemented"))
        {
            // During catalog derivation, an unimplemented status names a known
            // engine gap before its negative transcript is admitted. Once any
            // scenario is linked, the runner holds that evidence bidirectionally.
            // MARVEL-307 turns absence itself into the final module gate.
            if (obligation.Implementation == "unimplemented"
                && obligation.Scenarios.Count == 0)
            {
                continue;
            }

            if (obligation.Scenarios.Count == 0)
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no scenarios");
            }

            if (string.IsNullOrWhiteSpace(obligation.Mutation))
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no mutation evidence");
            }

            if (obligation.Implementation == "unimplemented"
                && string.IsNullOrWhiteSpace(obligation.Exception))
            {
                throw new TranscriptException(
                    $"unimplemented obligation '{obligation.Id}' names no expected exception");
            }

            foreach (string reference in obligation.Scenarios)
            {
                _ = references.Add(reference);
            }
        }

        return references;
    }

    internal static void ValidateScenarioCompleteness(
        IReadOnlySet<string> expected, IReadOnlySet<string> executed)
    {
        var missing = expected.Except(executed, StringComparer.Ordinal).ToList();
        var unexpected = executed.Except(expected, StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || unexpected.Count > 0)
        {
            string details = string.Join("; ", new[]
            {
                missing.Count == 0
                    ? null
                    : $"catalog scenarios not executed: {string.Join(", ", missing)}",
                unexpected.Count == 0
                    ? null
                    : $"executed scenarios absent from catalog: {string.Join(", ", unexpected)}",
            }.Where(value => value is not null));
            throw new TranscriptException(details);
        }
    }

    private static string Reference(TranscriptScenario scenario) =>
        $"{scenario.Location.Path}::{scenario.Name}";

    private static CatalogEvidence ReadCatalog(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        var sources = new Dictionary<string, CatalogSource>(StringComparer.Ordinal);
        var obligations = new Dictionary<string, CatalogObligation>(StringComparer.Ordinal);
        foreach (JsonElement source in document.RootElement
                     .GetProperty("sources").EnumerateArray())
        {
            string sourceId = source.GetProperty("id").GetString()!;
            sources.Add(sourceId, new CatalogSource(
                source.GetProperty("disposition").GetString()!));
            foreach (JsonElement obligation in source
                         .GetProperty("obligations").EnumerateArray())
            {
                string id = obligation.GetProperty("id").GetString()!;
                obligations.Add(id, new CatalogObligation(
                    id,
                    sourceId,
                    obligation.GetProperty("disposition").GetString()!,
                    obligation.GetProperty("implementation").GetString(),
                    [.. obligation.GetProperty("scenarios").EnumerateArray()
                        .Select(item => item.GetString()!)],
                    obligation.GetProperty("mutation").GetString(),
                    obligation.GetProperty("exception").GetString()));
            }
        }

        return new CatalogEvidence(sources, obligations);
    }
}

internal sealed record CatalogObligation(
    string Id,
    string Source,
    string Disposition,
    string? Implementation,
    IReadOnlyList<string> Scenarios,
    string? Mutation,
    string? Exception);

internal sealed record CatalogSource(string Disposition);

internal sealed record CatalogEvidence(
    IReadOnlyDictionary<string, CatalogSource> Sources,
    IReadOnlyDictionary<string, CatalogObligation> Obligations);
