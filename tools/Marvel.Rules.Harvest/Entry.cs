using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>One citable record of the Rules Reference.</summary>
/// <param name="Id">Its citation id — <c>rr:forced.4</c>.</param>
/// <param name="Path">The heading trail that leads to it.</param>
/// <param name="Text">The normative text, with the document's emphasis kept.</param>
public sealed record Record(string Id, IReadOnlyList<string> Path, string Text)
{
    /// <summary>The text with the emphasis taken out.</summary>
    public string Plain => Markdown.Plain(Text);

    /// <summary>
    /// The first sentence, which is what makes a citation legible in a diff.
    /// </summary>
    public string Fragment => Sentence.Match(Plain) is { Success: true } match
        ? match.Value.Trim()
        : Plain;

    /// <summary>
    /// The record's fingerprint — <c>sha256</c> of its text, emphasis removed.
    /// </summary>
    /// <remarks>
    /// <b>Over the plain text and not the marked-up text</b>, so that the
    /// document re-setting a word in bold does not read as the rule changing.
    /// Which is our choice: the Rules Reference has no fingerprint of its own,
    /// and the only property this one has to keep is that it moves when the
    /// rule does.
    /// </remarks>
    public string Hash =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Plain)))
            .ToLowerInvariant();

    // A sentence ends at a stop, a question mark, or the end of the text --
    // and not at the stop inside "e.g." or a numbered "1." because neither
    // is followed by a space and a capital.
    private static readonly Regex Sentence =
        new(@"^.*?[.?!](?=\s+[“""(\[]?[A-Z0-9]|\s*$)", RegexOptions.Singleline);
}

/// <summary>One glossary entry, and everything under it.</summary>
/// <param name="Id">Its citation id.</param>
/// <param name="Title">The heading, in the document's own casing.</param>
/// <param name="Page">The page its heading is on.</param>
/// <param name="Opening">The paragraphs before the first list.</param>
/// <param name="Steps">A numbered procedure, if it has one.</param>
/// <param name="Clauses">The numbered clauses, each with its qualifications.</param>
/// <param name="SeeAlso">The titles it cross-references, as printed.</param>
public sealed record Entry(
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
    public IEnumerable<Record> Records()
    {
        yield return new Record(Id, [Title], string.Join("\n\n", Opening));

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
            yield return new Record(
                at,
                [Title, $"clause {clause.Number.ToString(CultureInfo.InvariantCulture)}"],
                clause.Text);

            for (int under = 0; under < clause.Qualifications.Count; under++)
            {
                yield return new Record(
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

/// <summary>One clause of an entry, and its qualifications.</summary>
/// <param name="Number">Its position, which is its id.</param>
/// <param name="Text">The clause.</param>
/// <param name="Qualifications">The bullets under it.</param>
public sealed record Clause(int Number, string Text, IReadOnlyList<string> Qualifications);

/// <summary>
/// One step of a numbered procedure, and its lettered sub-steps.
/// </summary>
/// <remarks>
/// Named for the number rather than for the step, because <c>Step</c> is a
/// reserved word in one of the languages the .NET analysers care about.
/// </remarks>
/// <param name="Number">Its position.</param>
/// <param name="Text">The step.</param>
/// <param name="Substeps">The lettered steps under it, if any.</param>
public sealed record Numbered(int Number, string Text, IReadOnlyList<string> Substeps)
{
    /// <summary>This step and its sub-steps, as citable records.</summary>
    /// <param name="entry">The entry's id.</param>
    /// <param name="title">The entry's heading.</param>
    public IEnumerable<Record> Records(string entry, string title)
    {
        string number = Number.ToString(CultureInfo.InvariantCulture);
        yield return new Record($"{entry}.step.{number}", [title, $"step {number}"], Text);

        for (int under = 0; under < Substeps.Count; under++)
        {
            char letter = (char)('a' + under);
            yield return new Record(
                $"{entry}.step.{number}.{letter}",
                [title, $"step {number}", $"step {number}{letter}"],
                Substeps[under]);
        }
    }
}
