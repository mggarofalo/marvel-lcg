using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>One citable record of the Rules Reference.</summary>
/// <param name="Id">Its citation id — <c>rr:forced.4</c>.</param>
/// <param name="Path">The heading trail that leads to it.</param>
/// <param name="Text">The normative text, with the document's emphasis kept.</param>
public sealed record RuleRecord(string Id, IReadOnlyList<string> Path, string Text)
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
