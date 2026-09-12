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

    public (int Seat, string From, string To)? LastFormChange { get; set; }

    public IReadOnlySet<int>? LastCardOptions { get; set; }

    public bool? LastAvailability { get; set; }

    public string? LastInspectedFace { get; set; }

    public (int Card, string Resources)? LastResourceGeneration { get; set; }

    public Prompt? PendingPrompt { get; set; }

    public Game? Game { get; set; }

    public (int Seat, IReadOnlyList<int> Unshuffled, IReadOnlyList<int> After)?
        LastSetupDeckShuffle { get; set; }

    public World World => Scene?.World
        ?? throw new TranscriptException("a canonical Core scene has not been constructed");
}

internal sealed class CoreTranscriptRunner
{
    internal static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);
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
            ValidateStep(context, scenario, current);
            ExecuteStep(context, scenario, current);
        }
        ValidateCompletion(context, scenario);
        return new TranscriptResult(
            scenario.Obligation,
            $"{scenario.Location.Path}::{scenario.Name}",
            context.ExceptionDigest ?? context.World.Digest().Fingerprint(),
            [.. context.Events]);
    }

    private static void ValidateStep(
        TranscriptContext context, TranscriptScenario scenario,
        BoundTranscriptStep current)
    {
        if (context.ExceptionObserved)
            throw Failure(context, scenario, current.Step,
                TranscriptFailureKind.Validation,
                "unused step after the expected exception was observed", null);
        if (context.PendingException is not null
            && current.Binding.Name != "cataloged-exception")
            throw Failure(context, scenario, current.Step,
                TranscriptFailureKind.Validation,
                "the step after an unimplemented decision must observe its cataloged exception",
                null);
    }

    private static void ExecuteStep(
        TranscriptContext context, TranscriptScenario scenario,
        BoundTranscriptStep current)
    {
        try
        {
            current.Binding.Execute(context, current.Step, current.Match);
        }
        catch (RulesNotImplementedException error)
            when (current.Step.Kind == TranscriptStepKind.When
                && context.ExpectedException is not null)
        {
            ObserveExpectedException(context, scenario, current.Step, error);
        }
        catch (TranscriptAssertionException error)
        {
            throw Failure(context, scenario, current.Step,
                TranscriptFailureKind.Assertion, error.Message, error);
        }
        catch (TranscriptException error)
        {
            throw Failure(context, scenario, current.Step,
                TranscriptFailureKind.Execution, error.Message, error);
        }
        catch (Exception error)
        {
            throw Failure(context, scenario, current.Step,
                TranscriptFailureKind.Execution,
                $"{error.GetType().Name}: {error.Message}", error);
        }
    }

    private static void ObserveExpectedException(
        TranscriptContext context, TranscriptScenario scenario,
        TranscriptStep step, RulesNotImplementedException error)
    {
        if (context.Scene is null)
            throw Failure(context, scenario, step, TranscriptFailureKind.Execution,
                "unimplemented decision was reached before a legal scene existed", error);
        context.PendingException = error;
        context.ExceptionDigest = context.World.Digest().Fingerprint();
    }

    private static void ValidateCompletion(
        TranscriptContext context, TranscriptScenario scenario)
    {
        if (context.Scene is null)
            throw new TranscriptException(
                $"{scenario.Location}: {scenario.Obligation}: scenario never constructs a scene");
        if (context.ExpectedException is not null && !context.ExceptionObserved)
            throw new TranscriptException(
                $"{scenario.Location}: expected '{context.ExpectedException}', but the scenario completed");
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
        CoreTranscriptVocabulary.All();
}

internal static class TranscriptContextExtensions
{
    public static CanonicalCoreScene SceneRequired(
        this TranscriptContext context, TranscriptStep step) =>
        context.Scene ?? throw new TranscriptException(
            $"{step.Location}: a canonical Core scene must be dealt first");
}
