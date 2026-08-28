using System.Text.RegularExpressions;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One <c>[Rule]</c> attribute, and where it sits.</summary>
/// <param name="Id">The cited id.</param>
/// <param name="Site">The file it was found in, relative to the repository root.</param>
internal readonly record struct Cited(string Id, string Site);

/// <summary>
/// Every citation the test suite makes, read off the source.
/// </summary>
/// <remarks>
/// <para>
/// <b>The source and not the assemblies.</b> Reflection would be the more
/// precise answer and needs the suite built first, which makes a report of what
/// has been written depend on whether it currently compiles. The attribute is a
/// literal string on a line of its own in every use, so a reader over the text
/// answers the same question with nothing in the way.
/// </para>
/// <para>
/// <b>It does not validate.</b> A citation naming no rule is a build failure —
/// <c>RuleCitationTests.EveryCitedRuleExists</c> — and validating here as well
/// would put the same claim in two places, only one of which can fail a build.
/// What this does with an unknown id is count it, and say so.
/// </para>
/// </remarks>
internal static partial class Citations
{
    /// <summary>Reads every citation under <c>tests/</c>.</summary>
    public static IReadOnlyList<Cited> Read() =>
        Read(RepositoryPaths.Repository("tests"), RepositoryPaths.Root);

    /// <summary>Reads every citation under one source root.</summary>
    internal static IReadOnlyList<Cited> Read(string root, string repositoryRoot)
    {
        var found = new List<Cited>();

        foreach (string file in Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string site = Path.GetRelativePath(repositoryRoot, file)
                .Replace(Path.DirectorySeparatorChar, '/');

            foreach (Match match in Attribute().Matches(File.ReadAllText(file)))
            {
                found.Add(new Cited(match.Groups[1].Value, site));
            }
        }

        return found;
    }

    // An attribute is a literal on its own line. Anchoring that grammar keeps
    // documentation and comments from becoming claims the suite never made.
    [GeneratedRegex(
        @"^[ \t]*\[Rule\(""([^""]+)""\)\][ \t]*\r?$",
        RegexOptions.Multiline)]
    private static partial Regex Attribute();
}
