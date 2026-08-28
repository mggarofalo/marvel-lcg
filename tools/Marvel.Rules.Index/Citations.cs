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

            var state = SourceState.Code;
            int rawQuotes = 0;
            foreach (string line in File.ReadLines(file))
            {
                if (state == SourceState.Code && Attribute().Match(line) is { Success: true } match)
                {
                    found.Add(new Cited(match.Groups[1].Value, site));
                }

                Scan(line, ref state, ref rawQuotes);
            }
        }

        return found;
    }

    // An attribute is a literal on its own line. The named note is metadata on
    // the same claim, so it does not change the id the report reads.
    [GeneratedRegex(
        @"^[ \t]*\[Rule\(""([^""]+)""(?:,[ \t]*Note[ \t]*=[ \t]*""(?:\\.|[^""\\])*"")?\)\][ \t]*$")]
    private static partial Regex Attribute();

    /// <summary>Advances the lexical state through one physical source line.</summary>
    private static void Scan(string line, ref SourceState state, ref int rawQuotes)
    {
        for (int index = 0; index < line.Length; index += 1)
        {
            char current = line[index];
            char next = index + 1 < line.Length ? line[index + 1] : '\0';

            switch (state)
            {
                case SourceState.Code when current == '/' && next == '/':
                    return;
                case SourceState.Code when current == '/' && next == '*':
                    state = SourceState.BlockComment;
                    index += 1;
                    break;
                case SourceState.Code when current == '\'':
                    state = SourceState.Character;
                    break;
                case SourceState.Code when current == '"':
                    int quotes = QuotesAt(line, index);
                    if (quotes >= 3)
                    {
                        state = SourceState.RawString;
                        rawQuotes = quotes;
                        index += quotes - 1;
                    }
                    else
                    {
                        state = IsVerbatimPrefix(line, index)
                            ? SourceState.VerbatimString
                            : SourceState.String;
                    }

                    break;
                case SourceState.BlockComment when current == '*' && next == '/':
                    state = SourceState.Code;
                    index += 1;
                    break;
                case SourceState.String when current == '\\':
                case SourceState.Character when current == '\\':
                    index += 1;
                    break;
                case SourceState.String when current == '"':
                case SourceState.Character when current == '\'':
                    state = SourceState.Code;
                    break;
                case SourceState.VerbatimString when current == '"' && next == '"':
                    index += 1;
                    break;
                case SourceState.VerbatimString when current == '"':
                    state = SourceState.Code;
                    break;
                case SourceState.RawString when current == '"':
                    int closing = QuotesAt(line, index);
                    if (closing >= rawQuotes)
                    {
                        state = SourceState.Code;
                        index += rawQuotes - 1;
                        rawQuotes = 0;
                    }

                    break;
            }
        }
    }

    private static int QuotesAt(string line, int start)
    {
        int end = start;
        while (end < line.Length && line[end] == '"')
        {
            end += 1;
        }

        return end - start;
    }

    private static bool IsVerbatimPrefix(string line, int quote) =>
        quote > 0 && line[quote - 1] == '@'
        || quote > 1 && line[quote - 2] == '@' && line[quote - 1] == '$';

    private enum SourceState
    {
        Code,
        BlockComment,
        String,
        VerbatimString,
        RawString,
        Character,
    }
}
