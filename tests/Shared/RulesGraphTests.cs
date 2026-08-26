using System.Text.Json;
using Xunit;

namespace Marvel.Tests;

/// <summary>
/// The authored rule graph names rules that exist.
/// </summary>
/// <remarks>
/// <para>
/// <c>datasets/rules-graph.json</c> is hand-authored — "an exception names the
/// rule it overrides or extends; a base rule names nothing" — and every id in
/// it is a citation into the vendored Rules Reference, exactly as a
/// <see cref="RuleAttribute"/> is. It gets the same gate for the same reason:
/// when the Rules Reference is re-harvested and a clause is renumbered or
/// withdrawn, an edge pointing at it becomes a plausible-looking relationship
/// between a rule and nothing.
/// </para>
/// <para>
/// It is one file rather than three, so this lives in one project rather than
/// being linked into each the way <see cref="RuleCitationTests"/> is — that one
/// is per-assembly because each assembly makes its own citations.
/// </para>
/// </remarks>
public sealed class RulesGraphTests
{
    [Fact]
    public void EveryEdgeNamesARuleThatExists()
    {
        var unknown = Ids()
            .Where(id => !RuleCitations.Citable.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"Rules Reference v{RuleCitations.Version} has no such rule:"
            + Environment.NewLine + string.Join(Environment.NewLine, unknown));
    }

    [Fact]
    public void EveryEdgeSaysWhyItIsThere()
    {
        // The dataset's own note: "each edge records why, because a
        // plausible-but-wrong relationship is the failure mode this corpus
        // exists to eliminate." An edge with no reason is a claim nobody can
        // check, which is the thing it is warning about.
        using var graph = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));

        foreach (var edge in graph.RootElement.GetProperty("edges").EnumerateObject())
        {
            Assert.True(
                edge.Value.TryGetProperty("why", out var why)
                && (why.GetString() ?? string.Empty).Length > 0,
                $"{edge.Name} names another rule and does not say why");
        }
    }

    [Fact]
    public void NoRuleNamesItself()
    {
        using var graph = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));

        foreach (var edge in graph.RootElement.GetProperty("edges").EnumerateObject())
        {
            foreach (var to in edge.Value.GetProperty("references").EnumerateArray())
            {
                // An entry naming its own clause would be an edge that says
                // nothing: the graph is about one rule qualifying *another*.
                string target = to.GetString()!;
                int dot = target.IndexOf('.', StringComparison.Ordinal);
                string entry = dot < 0 ? target : target[..dot];
                Assert.False(
                    string.Equals(entry, edge.Name, StringComparison.Ordinal),
                    $"{edge.Name} names itself");
            }
        }
    }

    private static List<string> Ids()
    {
        using var graph = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));

        var found = new List<string>();
        foreach (var edge in graph.RootElement.GetProperty("edges").EnumerateObject())
        {
            found.Add(edge.Name);
            foreach (var to in edge.Value.GetProperty("references").EnumerateArray())
            {
                found.Add(to.GetString()!);
            }
        }

        return found;
    }
}
