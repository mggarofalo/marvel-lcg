using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>One glossary entry, and everything under it.</summary>
/// <param name="Id">Its citation id.</param>
/// <param name="Title">The heading, in the document's own casing.</param>
/// <param name="Page">The page its heading is on.</param>
/// <param name="Opening">The paragraphs before the first list.</param>
/// <param name="Steps">A numbered procedure, if it has one.</param>
/// <param name="Clauses">The numbered clauses, each with its qualifications.</param>
/// <param name="SeeAlso">The titles it cross-references, as printed.</param>
public sealed record RulesReferenceEntry(
    string Id,
    string Title,
    int Page,
    IReadOnlyList<string> Opening,
    IReadOnlyList<Numbered> Steps,
    IReadOnlyList<Clause> Clauses,
    IReadOnlyList<string> SeeAlso)
{
    /// <summary>
    /// The id a heading gets — <c>ATTACK (ENEMY ACTIVATION)</c> becomes
    /// <c>rr:attack-enemy-activation</c>.
    /// </summary>
    /// <remarks>
    /// Quotation marks go and everything else that is not a letter or a digit
    /// becomes a separator, so a parenthesis reads as one and an apostrophe
    /// does too: <c>PLAYER’S PLAY AREA</c> is <c>rr:player-s-play-area</c>.
    /// <b>Positional rather than derived from the text</b> below the heading,
    /// so that a citation survives a rewording — which is exactly when it most
    /// needs to.
    /// </remarks>
    /// <param name="title">A heading.</param>
    public static string Slug(string title)
    {
        // An icon printed beside a heading -- "CRISIS ICON ([crisis])" -- names
        // the glyph the entry is about and not a second word of its name.
        string bare = Icon.Replace(title, string.Empty)
            .Replace("“", string.Empty, StringComparison.Ordinal)
            .Replace("”", string.Empty, StringComparison.Ordinal)
            .Replace("\"", string.Empty, StringComparison.Ordinal);

        var slug = new StringBuilder("rr:");
        bool separated = true;
        foreach (char letter in bare)
        {
            if (char.IsAsciiLetterOrDigit(letter))
            {
                slug.Append(char.ToLowerInvariant(letter));
                separated = false;
            }
            else if (!separated)
            {
                slug.Append('-');
                separated = true;
            }
        }

        return slug.ToString().TrimEnd('-');
    }

    private static readonly Regex Icon = new(@"\[[a-z-]+\]");

    /// <summary>Every citable record this entry holds, itself first.</summary>
    public IEnumerable<RuleRecord> Records()
    {
        yield return new RuleRecord(Id, [Title], string.Join("\n\n", Opening));

        foreach (var step in Steps)
        {
            foreach (var record in step.Records(Id, Title))
            {
                yield return record;
            }
        }

        foreach (var clause in Clauses)
        {
            string at = $"{Id}.{clause.Number.ToString(CultureInfo.InvariantCulture)}";
            yield return new RuleRecord(
                at,
                [Title, $"clause {clause.Number.ToString(CultureInfo.InvariantCulture)}"],
                clause.Text);

            for (int under = 0; under < clause.Qualifications.Count; under++)
            {
                yield return new RuleRecord(
                    $"{at}.{(under + 1).ToString(CultureInfo.InvariantCulture)}",
                    [
                        Title,
                        $"clause {clause.Number.ToString(CultureInfo.InvariantCulture)}",
                        $"qualification {(under + 1).ToString(CultureInfo.InvariantCulture)}",
                    ],
                    clause.Qualifications[under]);
            }
        }
    }
}
