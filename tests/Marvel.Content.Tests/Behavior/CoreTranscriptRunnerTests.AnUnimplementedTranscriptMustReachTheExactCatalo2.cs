using System.Text.RegularExpressions;
using Marvel.Behavior.Run;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CoreTranscriptRunnerAnUnimplementedTranscriptMustReachTheExactCataloTests : CoreTranscriptRunnerTestBase
{
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
        TranscriptScenario scenario = Assert.Single(TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        IReadOnlyList<TranscriptBinding> bindings = [..CoreTranscriptRunner.DefaultVocabulary(), new TranscriptBinding("activation-rule-5", TranscriptStepKind.When, new Regex("\\Athe engine reaches activation rule 5\\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), (_, _, _) => throw new RulesNotImplementedException("rr:activation.5")), ];
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);
        TranscriptResult result = runner.Execute(scenario, "RulesNotImplementedException: rr:activation.5");
        Assert.Matches("^[0-9a-f]{64}$", result.Digest);
        TranscriptException mismatch = Assert.Throws<TranscriptException>(() => runner.Execute(scenario, "RulesNotImplementedException: some other rule"));
        Assert.Contains("expected 'RulesNotImplementedException: some other rule'", mismatch.Message, StringComparison.Ordinal);
        Assert.Contains("reached 'RulesNotImplementedException: rr:activation.5'", mismatch.Message, StringComparison.Ordinal);
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
        TranscriptScenario scenario = Assert.Single(TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        IReadOnlyList<TranscriptBinding> bindings = [..CoreTranscriptRunner.DefaultVocabulary(), new TranscriptBinding("activation-rule-5", TranscriptStepKind.When, new Regex("\\Athe engine reaches activation rule 5\\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), (_, _, _) => throw new RulesNotImplementedException("rr:activation.5")), ];
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);
        TranscriptException failure = Assert.Throws<TranscriptException>(() => runner.Execute(scenario, "RulesNotImplementedException: rr:activation.5"));
        Assert.Equal(TranscriptFailureKind.UnknownStep, failure.Kind);
        Assert.Contains("unknown Then step", failure.Message, StringComparison.Ordinal);
        Assert.Contains("world-digest: <scene not constructed>", failure.Message, StringComparison.Ordinal);
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
        TranscriptException failure = Assert.Throws<TranscriptException>(() => TranscriptParser.Parse(feature.Root, feature.Path));
        Assert.Contains("duplicate scenario name 'duplicate'", failure.Message, StringComparison.Ordinal);
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
        TranscriptException failure = Assert.Throws<TranscriptException>(() => TranscriptParser.Parse(feature.Root, feature.Path));
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }
}
