using Marvel.Tests;
using Gherkin;
using Xunit;

namespace Marvel.Core.Tests.Specs;

/// <summary>
/// Do all candidate and admitted specs parse under the Gherkin the C# side uses?
/// </summary>
/// <remarks>
/// <para>
/// The <c>.feature</c> files under <c>specs/</c> have to stay loadable by
/// whatever runs them, so this parses every one under the standard grammar. If
/// they drift from it, every scenario has to be rewritten, and the cheapest
/// moment to learn that is before the suite grows further.
/// </para>
/// <para>
/// <b>This checks the format and says nothing about the behaviour.</b> Files
/// under <c>cards/</c> and <c>rules/</c> remain drafts. This test does not confer
/// admission; the Rules Reference decides what the game does. See
/// <c>specs/README.md</c>.
/// </para>
/// <para>
/// This uses the <c>Gherkin</c> package directly, which is the parser Reqnroll
/// is built on. Referenced instead of the full BDD runner deliberately: the
/// question is whether the *format* is compatible. The admitted corpus instead
/// uses the small typed vocabulary in <c>Marvel.Behavior.Run</c>.
/// </para>
/// </remarks>
public sealed class GherkinFormatTests
{
    private static IReadOnlyList<string> FeatureFiles { get; } =
        [.. Directory.EnumerateFiles(
                Path.Combine(RepositoryPaths.Root, "specs"),
                "*.feature", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)];

    [Fact]
    public void TheSpecSuiteIsWhereItIsExpected()
    {
        // Without this, a moved directory turns every test below into a
        // vacuous pass over an empty list.
        Assert.True(FeatureFiles.Count >= 100,
            $"expected the spec suite, found {FeatureFiles.Count} feature file(s)");
    }

    [Fact]
    public void EveryFeatureFileParses()
    {
        var parser = new Parser();
        var failures = new List<string>();

        foreach (string path in FeatureFiles)
        {
            try
            {
                parser.Parse(path);
            }
            catch (Exception exception) when (exception is CompositeParserException or ParserException)
            {
                string relative = Path.GetRelativePath(RepositoryPaths.Root, path);
                failures.Add($"{relative}: {exception.Message.Split('\n')[0]}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void EveryFeatureHasScenariosAndEveryScenarioHasSteps()
    {
        // A file that parses but yields nothing would pass the test above while
        // meaning the format is not actually understood.
        var parser = new Parser();
        var empty = new List<string>();

        foreach (string path in FeatureFiles)
        {
            var document = parser.Parse(path);
            string relative = Path.GetRelativePath(RepositoryPaths.Root, path);

            var children = document.Feature?.Children.ToList() ?? [];
            var scenarios = children.OfType<Gherkin.Ast.Scenario>().ToList();
            if (scenarios.Count == 0)
            {
                empty.Add($"{relative}: no scenarios");
                continue;
            }

            foreach (var scenario in scenarios.Where(s => !s.Steps.Any()))
            {
                empty.Add($"{relative}: '{scenario.Name}' has no steps");
            }
        }

        Assert.Empty(empty);
    }

    [Fact]
    public void TagsSurviveTheParse()
    {
        // The `@card:` and `@rr:` tags are how a scenario is joined to the card
        // dataset and to the Rules Reference. A parser that dropped or mangled
        // them would take the whole citation graph with it (MARVEL-154).
        var parser = new Parser();
        var tags = new HashSet<string>(StringComparer.Ordinal);

        foreach (string path in FeatureFiles)
        {
            var document = parser.Parse(path);
            foreach (var scenario in (document.Feature?.Children ?? [])
                     .OfType<Gherkin.Ast.Scenario>())
            {
                foreach (var tag in scenario.Tags)
                {
                    tags.Add(tag.Name);
                }
            }
        }

        Assert.Contains(tags, tag => tag.StartsWith("@card:", StringComparison.Ordinal));
        Assert.Contains(tags, tag => tag.StartsWith("@rr:", StringComparison.Ordinal));
    }

    [Fact]
    public void EscapedQuotesInCardNamesSurviveTheParse()
    {
        // Gherkin treats a step as opaque text. The repository chooses the
        // backslash spelling, and future bindings decode it after this parse.
        var parser = new Parser();
        var stepTexts = FeatureFiles
            .Select(parser.Parse)
            .SelectMany(document => document.Feature?.Children ?? [])
            .OfType<Gherkin.Ast.Scenario>()
            .SelectMany(scenario => scenario.Steps)
            .Select(step => step.Text)
            .ToList();

        Assert.Contains("\"\\\"I'm Tough\\\"\" is revealed", stepTexts);
        Assert.Contains(
            "\"The \\\"Immortal\\\" Klaw\" is in the \"SideSchemesArea\"",
            stepTexts);
        Assert.Contains(
            "I thwart \"The \\\"Immortal\\\" Klaw\"",
            stepTexts);
    }
}
