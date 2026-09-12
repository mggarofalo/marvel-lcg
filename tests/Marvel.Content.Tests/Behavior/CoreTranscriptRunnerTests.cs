using System.Text.RegularExpressions;
using Marvel.Behavior.Run;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public abstract class CoreTranscriptRunnerTestBase
{
    private protected static TranscriptException ExecuteSynthetic(string observation, IReadOnlyList<TranscriptBinding> bindings)
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
        TranscriptScenario scenario = Assert.Single(TranscriptParser.Parse(feature.Root, feature.Path).Scenarios);
        var runner = new CoreTranscriptRunner(RepositoryPaths.Root, bindings);
        return Assert.Throws<TranscriptException>(() => runner.Execute(scenario));
    }

    protected sealed class TemporaryFeature : IDisposable
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
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"marvel-behavior-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            string path = System.IO.Path.Combine(root, "test.feature");
            File.WriteAllText(path, text);
            return new TemporaryFeature(root, path);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
