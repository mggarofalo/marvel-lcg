using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Marvel.Tests;

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

        using var graph = JsonDocument.Parse(
            File.ReadAllBytes(RepositoryPaths.Dataset("rules-graph.json")));
        foreach (var modification in graph.RootElement.GetProperty("modifications").EnumerateObject())
        {
            ids.Add(modification.Name);
        }

        return ids;
    }
}
