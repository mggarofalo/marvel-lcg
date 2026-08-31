using System.Text.RegularExpressions;
using Marvel.Behavior.Run;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class CoreTranscriptRunnerTests
{
    [Fact]
    public void AuthorityDerivedPlayerDeckTranscriptRunsEndToEnd()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);

        TranscriptResult result = Assert.Single(suite.RunPassingCorpus());

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
    public void QuarantineFailsOnItsFalseObservationAndIsNotInThePassingCorpus()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);

        TranscriptException failure = suite.RunQuarantine();

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
                  | next card |
                  | 01006     |
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
