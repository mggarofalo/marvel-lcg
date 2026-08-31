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
    string Digest,
    IReadOnlyList<GameEvent> Events);

internal sealed record TranscriptBinding(
    string Name,
    TranscriptStepKind Kind,
    Regex Pattern,
    Action<TranscriptContext, TranscriptStep, Match> Execute);

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

    public TranscriptResult Execute(TranscriptScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var context = new TranscriptContext(scenario.Obligation, setup, cards, abilities);
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
                throw Failure(context, scenario, step, reason, null);
            }

            try
            {
                matches[0].Binding.Execute(context, step, matches[0].Match);
            }
            catch (TranscriptException error)
            {
                throw Failure(context, scenario, step, error.Message, error);
            }
            catch (Exception error)
            {
                throw Failure(context, scenario, step,
                    $"{error.GetType().Name}: {error.Message}", error);
            }
        }

        if (context.Scene is null)
        {
            throw new TranscriptException(
                $"{scenario.Location}: {scenario.Obligation}: scenario never constructs a scene");
        }

        return new TranscriptResult(
            scenario.Obligation,
            context.World.Digest().Fingerprint(),
            [.. context.Events]);
    }

    internal static IReadOnlyList<TranscriptBinding> DefaultVocabulary() =>
    [
        Bind("core-scene", TranscriptStepKind.Given,
            "a canonical Core scene is dealt", DealScene),
        Bind("stack-player-deck", TranscriptStepKind.Given,
            @"seat (?<seat>\d+)'s player deck contains only these next cards", StackPlayerDeck),
        Bind("draw-cards", TranscriptStepKind.When,
            @"seat (?<seat>\d+) draws (?<count>\d+) cards?", DrawCards),
        Bind("hand-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in hand", HandCount),
        Bind("player-deck-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) cards? in their player deck", PlayerDeckCount),
        Bind("encounter-queue-count", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) has (?<count>\d+) facedown encounter cards?", EncounterCount),
        Bind("not-eliminated", TranscriptStepKind.Then,
            @"seat (?<seat>\d+) is not eliminated", NotEliminated),
        Bind("game-unfinished", TranscriptStepKind.Then,
            "the game is unfinished", GameUnfinished),
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
    {
        int seat = Seat(match, step);
        TranscriptTable table = Table(step, "next card");
        context.SceneRequired(step).Apply(new StackPlayerDeck(
            seat,
            [.. table.Rows.Select(row => new SceneCard(row["next card"]))],
            DiscardOthers: true));
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

    private static void EncounterCount(
        TranscriptContext context, TranscriptStep step, Match match) =>
        Equal(Number(match, "count", step),
            context.World.AreaOf(
                DeckType.DealtEncounterCardsDeck,
                PlayArea.Of(Seat(match, step))).Cards.Count,
            "facedown encounter cards", step);

    private static void NotEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptException($"{step.Location}: expected seat {seat + 1} not to be eliminated");
        }
    }

    private static void GameUnfinished(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.Unfinished)
        {
            throw new TranscriptException(
                $"{step.Location}: expected an unfinished game; was {context.World.Result}");
        }
    }

    private static void Equal(int expected, int actual, string observation, TranscriptStep step)
    {
        if (actual != expected)
        {
            throw new TranscriptException(
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
        return inner is null
            ? new TranscriptException(message)
            : new TranscriptException(message, inner);
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
    private readonly HashSet<string> obligations;
    private readonly CoreTranscriptRunner runner;

    public CoreTranscriptSuite(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        runner = new CoreTranscriptRunner(this.root);
        obligations = ReadObligations(Path.Combine(
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
        foreach (string path in paths)
        {
            TranscriptFeature feature = TranscriptParser.Parse(root, path);
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                ValidateAuthority(scenario);
                results.Add(runner.Execute(scenario));
            }
        }

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
                ValidateAuthority(scenario);
                _ = runner.Execute(scenario);
            }
        }
        catch (TranscriptException expected)
        {
            return expected;
        }

        throw new TranscriptException(
            "specs/self-test/quarantine.feature passed; its false assertion no longer proves the runner");
    }

    private void ValidateAuthority(TranscriptScenario scenario)
    {
        if (!obligations.Contains(scenario.Obligation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: stale or missing obligation '{scenario.Obligation}'");
        }

        if (scenario.Authorities.Count == 0)
        {
            throw new TranscriptException(
                $"{scenario.Location}: scenario has no direct authority tags");
        }
    }

    private static HashSet<string> ReadObligations(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("sources").EnumerateArray()
            .SelectMany(source => source.GetProperty("obligations").EnumerateArray())
            .Select(obligation => obligation.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
