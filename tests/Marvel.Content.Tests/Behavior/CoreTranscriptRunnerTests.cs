using System.Text.RegularExpressions;
using Marvel.Behavior.Run;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreTranscriptRunnerTests
{
    [Fact]
    public void AuthorityDerivedPlayerDeckTranscriptRunsEndToEnd()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);

        TranscriptResult result = suite.RunScenario(
            "specs/behavior/core/player-deck-empty.feature",
            "Drawing continues through the player-deck reset");

        Assert.Equal("behavior:rr:player-deck.2:published-result", result.Obligation);
        Assert.Contains(result.Events, gameEvent =>
            gameEvent.GetType().Name == "AreaReordered");
        Assert.Contains(result.Events, gameEvent =>
            gameEvent.GetType().Name == "CardsMoved");
        Assert.Equal(
            "630f931c433098646b8aaeb96e9baa0f7df8b9a95db6786153607231f57fca45",
            result.Digest);
    }

    [Fact]
    public void EveryAuthoredCoreSetupRecordRunsFromItsCanonicalScene()
    {
        string path = Path.Combine(
            RepositoryPaths.Root, "specs", "behavior", "core",
            "setup-authorities.feature");
        TranscriptFeature feature = TranscriptParser.Parse(
            RepositoryPaths.Root, path);
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root);

        List<TranscriptResult> results =
            feature.Scenarios.Select(runner.Execute).ToList();

        Assert.Equal(18, results.Count);
        Assert.Equal(
            18,
            results.Select(result => result.Obligation)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.All(results, result =>
            Assert.Matches("^[0-9a-f]{64}$", result.Digest));
    }

    [Fact]
    public void QuarantineFailsOnItsFalseObservationAndIsNotInThePassingCorpus()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);

        TranscriptException failure = suite.RunQuarantine();

        Assert.Equal(TranscriptFailureKind.Assertion, failure.Kind);
        Assert.Contains("expected 99 cards in hand; was 7", failure.Message,
            StringComparison.Ordinal);
        Assert.Contains("obligation: behavior:rr:player-deck.2:published-result",
            failure.Message, StringComparison.Ordinal);
        Assert.Contains("world-digest: ", failure.Message, StringComparison.Ordinal);
        Assert.Contains("current-prompt: <none>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("recent-events:", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AndRetainsThePreviousStepKindAndTablesAreParsed()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Parse the transcript surface
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                And seat 1's player deck contains only these next cards
                  | next card | copy |
                  | 01006     | 0    |
                When seat 1 draws 2 cards
                Then seat 1 has 7 cards in hand
                And the game is unfinished
            """);

        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);

        Assert.Equal(
            [TranscriptStepKind.Given, TranscriptStepKind.Given,
             TranscriptStepKind.When, TranscriptStepKind.Then, TranscriptStepKind.Then],
            scenario.Steps.Select(step => step.Kind));
        Assert.Equal("01006", scenario.Steps[1].Table!.Rows[0]["next card"]);
    }

    [Fact]
    public void ATranscriptCanDeclareNecessarilyObservedSecondaryObligations()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Declare co-coverage
              @behavior:rr:encounter-deck.1:empty-with-discard
              @covers:behavior:rr:acceleration-token.1:published-result
              @rr:encounter-deck.1 @rr:acceleration-token.1
              Scenario: one decision observes both rules
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
                Then the game is unfinished
            """);

        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);

        Assert.Equal("behavior:rr:encounter-deck.1:empty-with-discard", scenario.Obligation);
        Assert.Equal(
            ["behavior:rr:acceleration-token.1:published-result"],
            scenario.CoveredObligations);
    }

    [Fact]
    public void UnknownStepsFailAtTheirFeatureLine()
    {
        TranscriptException failure = ExecuteSynthetic(
            "Then seat 1 has a pony",
            CoreTranscriptRunner.DefaultVocabulary());

        Assert.Contains("unknown Then step 'seat 1 has a pony'", failure.Message,
            StringComparison.Ordinal);
        Assert.Contains("line: 8", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousStepsNameEveryMatchingBinding()
    {
        IReadOnlyList<TranscriptBinding> defaults =
            CoreTranscriptRunner.DefaultVocabulary();
        var bindings = defaults.Concat(
        [
            new TranscriptBinding(
                "duplicate-hand-count",
                TranscriptStepKind.Then,
                new Regex(
                    @"\Aseat (?<seat>\d+) has (?<count>\d+) cards? in hand\z",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)),
                (_, _, _) => { }),
        ]).ToList();

        TranscriptException failure = ExecuteSynthetic(
            "Then seat 1 has 8 cards in hand",
            bindings);

        Assert.Contains("ambiguous Then step", failure.Message, StringComparison.Ordinal);
        Assert.Contains("hand-count", failure.Message, StringComparison.Ordinal);
        Assert.Contains("duplicate-hand-count", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnusedTableColumnIsRejected()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Reject unused input
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed | ignored |
                  | rhino    | spider_man | 303  | no      |
                When seat 1 draws 1 card
                Then the game is unfinished
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root);

        TranscriptException failure = Assert.Throws<TranscriptException>(
            () => runner.Execute(scenario));

        Assert.Contains("unused columns: ignored", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AStackCanSelectDistinctCopiesOfTheSamePrintedCard()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Select physical copies
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes         | seed |
                  | rhino    | captain_marvel | 303  |
                And seat 1's player deck contains only these next cards
                  | next card | copy |
                  | 01012     | 0    |
                  | 01012     | 1    |
                When seat 1 draws 2 cards
                Then the game is unfinished
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root);

        TranscriptResult result = runner.Execute(scenario);

        Assert.NotEmpty(result.Digest);
    }

    [Fact]
    public void ADecisionWithoutAnObservationIsRejected()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Reject assertion-free behavior
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
            """);

        TranscriptException failure = Assert.Throws<TranscriptException>(
            () => TranscriptParser.Parse(feature.Root, feature.Path));

        Assert.Contains("final When has no observable Then", failure.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "@behavior:rr:player-deck.2:published-result @rr:does-not-exist",
        "missing direct authorities: rr:does-not-exist")]
    [InlineData(
        "@behavior:rr:player-deck.2:published-result @rr:draw-drawing-cards",
        "primary obligation derives from 'rr:player-deck.2'")]
    [InlineData(
        "@behavior:rr:campaign-specific-card:source-disposition @rr:campaign-specific-card",
        "outside-Core direct authorities: rr:campaign-specific-card")]
    [InlineData(
        "@behavior:rr:player-deck.2:published-result @rr:player-deck.2 @rr:campaign-specific-card",
        "outside-Core direct authorities: rr:campaign-specific-card")]
    public void PassingAuthorityTagsAndCatalogEvidenceAreValidated(
        string tags, string expected)
    {
        using var feature = TemporaryFeature.Create($$"""
            Feature: Reject false authority
              {{tags}}
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
                Then the game is unfinished
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);

        TranscriptException failure = Assert.Throws<TranscriptException>(
            () => suite.ValidateForPassing(scenario));

        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompletedCatalogEvidenceIsCheckedInReverse()
    {
        var unverified = new CatalogObligation(
            "behavior:rr:test:unverified", "rr:test", "executable", "unverified",
            [], null, null);

        TranscriptException incomplete = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.CompletedScenarioReferences([unverified]));

        Assert.Contains("is not completed; implementation is unverified", incomplete.Message,
            StringComparison.Ordinal);

        var missingScenario = new CatalogObligation(
            "behavior:rr:test:branch", "rr:test", "executable", "supported",
            [], "a mutation", null);

        TranscriptException absent = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.CompletedScenarioReferences([missingScenario]));

        Assert.Contains("has no scenarios", absent.Message, StringComparison.Ordinal);

        var missingMutation = missingScenario with
        {
            Scenarios = ["specs/behavior/core/test.feature::branch"],
            Mutation = null,
        };
        TranscriptException untested = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.CompletedScenarioReferences([missingMutation]));

        Assert.Contains("has no mutation evidence", untested.Message,
            StringComparison.Ordinal);

        var untestedNegative = missingMutation with
        {
            Implementation = "unimplemented",
            Exception = "RulesNotImplementedException: rr:test",
        };
        TranscriptException negative = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.CompletedScenarioReferences([untestedNegative]));

        Assert.Contains("has no mutation evidence", negative.Message,
            StringComparison.Ordinal);

        var untranscribedNegative = untestedNegative with
        {
            Scenarios = [],
            Mutation = "remove the refusal",
        };
        TranscriptException absentNegative = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.CompletedScenarioReferences([untranscribedNegative]));

        Assert.Contains("has no scenarios", absentNegative.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogAndExecutedScenarioReferencesMustMatchBothWays()
    {
        var expected = new HashSet<string>(["expected"], StringComparer.Ordinal);
        var executed = new HashSet<string>(["unexpected"], StringComparer.Ordinal);

        TranscriptException failure = Assert.Throws<TranscriptException>(() =>
            CoreTranscriptSuite.ValidateScenarioCompleteness(expected, executed));

        Assert.Contains("catalog scenarios not executed: expected", failure.Message,
            StringComparison.Ordinal);
        Assert.Contains("executed scenarios absent from catalog: unexpected", failure.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnimplementedTranscriptMustReachTheExactCatalogedException()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Observe an unimplemented rule
              @behavior:rr:activation.5:published-result @rr:activation.5
              Scenario: negative branch
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When the engine reaches activation rule 5
                Then the engine raises the cataloged unimplemented rule exception
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        IReadOnlyList<TranscriptBinding> bindings =
        [
            .. CoreTranscriptRunner.DefaultVocabulary(),
            new TranscriptBinding(
                "activation-rule-5",
                TranscriptStepKind.When,
                new Regex(
                    "\\Athe engine reaches activation rule 5\\z",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)),
                (_, _, _) => throw new RulesNotImplementedException("rr:activation.5")),
        ];
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);

        TranscriptResult result = runner.Execute(
            scenario, "RulesNotImplementedException: rr:activation.5");

        Assert.Matches("^[0-9a-f]{64}$", result.Digest);

        TranscriptException mismatch = Assert.Throws<TranscriptException>(() => runner.Execute(
            scenario, "RulesNotImplementedException: some other rule"));

        Assert.Contains("expected 'RulesNotImplementedException: some other rule'",
            mismatch.Message, StringComparison.Ordinal);
        Assert.Contains("reached 'RulesNotImplementedException: rr:activation.5'",
            mismatch.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryNegativeTranscriptStepIsBoundBeforeItsDecisionRuns()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Reject a malformed negative transcript
              @behavior:rr:activation.5:published-result @rr:activation.5
              Scenario: negative branch
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When the engine reaches activation rule 5
                Then this trailing assertion has no binding
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        IReadOnlyList<TranscriptBinding> bindings =
        [
            .. CoreTranscriptRunner.DefaultVocabulary(),
            new TranscriptBinding(
                "activation-rule-5",
                TranscriptStepKind.When,
                new Regex(
                    "\\Athe engine reaches activation rule 5\\z",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)),
                (_, _, _) => throw new RulesNotImplementedException("rr:activation.5")),
        ];
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);

        TranscriptException failure = Assert.Throws<TranscriptException>(() => runner.Execute(
            scenario, "RulesNotImplementedException: rr:activation.5"));

        Assert.Equal(TranscriptFailureKind.UnknownStep, failure.Kind);
        Assert.Contains("unknown Then step", failure.Message, StringComparison.Ordinal);
        Assert.Contains("world-digest: <scene not constructed>", failure.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateScenarioNamesCannotCollapseToOneCatalogReference()
    {
        using var feature = TemporaryFeature.Create("""
            Feature: Reject duplicate identities
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: duplicate
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
                Then the game is unfinished

              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: duplicate
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
                Then the game is unfinished
            """);

        TranscriptException failure = Assert.Throws<TranscriptException>(
            () => TranscriptParser.Parse(feature.Root, feature.Path));

        Assert.Contains("duplicate scenario name 'duplicate'", failure.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("And seat 1 draws 1 card", "And has no preceding step kind")]
    [InlineData("Then the game is unfinished", "Then requires a preceding When")]
    public void OrderInvalidStepsAreRejected(string step, string expected)
    {
        using var feature = TemporaryFeature.Create($$"""
            Feature: Reject invalid order
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                {{step}}
            """);

        TranscriptException failure = Assert.Throws<TranscriptException>(
            () => TranscriptParser.Parse(feature.Root, feature.Path));

        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }

    private static TranscriptException ExecuteSynthetic(
        string observation, IReadOnlyList<TranscriptBinding> bindings)
    {
        using var feature = TemporaryFeature.Create($$"""
            Feature: Exercise a binding failure
              @behavior:rr:player-deck.2:published-result @rr:player-deck.2
              Scenario: one scenario
                Given a canonical Core scene is dealt
                  | campaign | heroes     | seed |
                  | rhino    | spider_man | 303  |
                When seat 1 draws 1 card
                {{observation}}
            """);
        TranscriptScenario scenario = Assert.Single(
            TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);
        return Assert.Throws<TranscriptException>(() => runner.Execute(scenario));
    }

    private sealed class TemporaryFeature : IDisposable
    {
        private TemporaryFeature(string root, string path)
        {
            Root = root;
            Path = path;
        }

        public string Root { get; }

        public string Path { get; }

        public static TemporaryFeature Create(string text)
        {
            string root = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"marvel-behavior-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            string path = System.IO.Path.Combine(root, "test.feature");
            File.WriteAllText(path, text);
            return new TemporaryFeature(root, path);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
