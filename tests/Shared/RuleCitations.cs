using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Marvel.Tests;

/// <summary>Where a <see cref="RuleAttribute"/> sits, and what it cites.</summary>
/// <param name="Id">The cited id.</param>
/// <param name="Site">The type or method carrying the citation, for a failure message.</param>
internal readonly record struct Citation(string Id, string Site);

/// <summary>
/// Reads the Rules Reference index, and the citations one test assembly makes
/// against it.
/// </summary>
internal static class RuleCitations
{
    /// <summary>Every citable id in the vendored Rules Reference.</summary>
    /// <remarks>
    /// Entry ids and clause ids alike — a test may cite either, because a rule
    /// with no clauses (<c>rr:target-threat</c>) is stated in its entry.
    /// </remarks>
    public static IReadOnlySet<string> Citable { get; } = ReadIndex();

    /// <summary>The Rules Reference version the citations are against.</summary>
    public static string Version { get; private set; } = "unknown";

    /// <summary>Every citation made by one assembly.</summary>
    /// <param name="assembly">A test assembly.</param>
    public static IReadOnlyList<Citation> In(Assembly assembly)
    {
        var found = new List<Citation>();
        foreach (var type in assembly.GetTypes())
        {
            foreach (var rule in type.GetCustomAttributes<RuleAttribute>())
            {
                found.Add(new Citation(rule.Id, type.Name));
            }

            const BindingFlags Any =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var method in type.GetMethods(Any))
            {
                foreach (var rule in method.GetCustomAttributes<RuleAttribute>())
                {
                    found.Add(new Citation(rule.Id, $"{type.Name}.{method.Name}"));
                }
            }
        }

        return [.. found.OrderBy(c => c.Site, StringComparer.Ordinal)
                        .ThenBy(c => c.Id, StringComparer.Ordinal)];
    }

    private static HashSet<string> ReadIndex()
    {
        using var stream = File.OpenRead(
            RepositoryPaths.Dataset("rules-reference", "index.json"));
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        Version = root.TryGetProperty("version", out var version)
            ? version.GetString() ?? "unknown"
            : "unknown";

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("entries").EnumerateArray())
        {
            ids.Add(entry.GetProperty("id").GetString()!);
        }

        return ids;
    }
}

/// <summary>
/// The citations in this assembly name rules that exist.
/// </summary>
/// <remarks>
/// Linked into each test project, so each one holds its own citations. This is
/// the whole mechanism by which a <see cref="RuleAttribute"/> is a citation
/// rather than a comment: when the Rules Reference is re-harvested and a clause
/// is renumbered or withdrawn, the build says so.
/// </remarks>
public sealed class RuleCitationTests
{
    [Fact]
    public void EveryCitedRuleExists()
    {
        var citations = RuleCitations.In(typeof(RuleCitationTests).Assembly);
        var unknown = citations
            .Where(citation => !RuleCitations.Citable.Contains(citation.Id))
            .Select(citation => $"{citation.Site} cites {citation.Id}")
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"Rules Reference v{RuleCitations.Version} has no such rule:"
            + Environment.NewLine + string.Join(Environment.NewLine, unknown));
    }

    [Fact]
    public void CitationsAreWellFormed()
    {
        // `rr:` is the only citation scheme the index carries. A test citing
        // `pack:mc11:game-areas` is citing a rules *pack*, which is a different
        // dataset and not covered here -- catching that as a typo would be
        // wrong, so it has to be caught as a scheme.
        var citations = RuleCitations.In(typeof(RuleCitationTests).Assembly);
        Assert.DoesNotContain(
            citations, citation => !citation.Id.StartsWith("rr:", StringComparison.Ordinal));
    }
}
