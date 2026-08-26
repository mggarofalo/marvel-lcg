using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>Turning styled runs into text, and back out of it.</summary>
public static partial class Markdown
{
    /// <summary>One line's runs, as marked-up text.</summary>
    /// <param name="runs">The runs.</param>
    public static string Of(IEnumerable<Run> runs)
    {
        var written = new StringBuilder();
        var all = Merged(runs).ToList();
        for (int at = 0; at < all.Count; at++)
        {
            var run = Enclosed(all, at);

            // The document sets a marker in the same weight as the text it
            // introduces, so a bold run can be all whitespace. Marking that up
            // would emit `** **`, which is emphasis around nothing.
            //
            // **The same for punctuation.** A bracket that opens an italic
            // aside is set in italic too, and `*(*` is not emphasis around
            // anything a reader can see. What emphasis means is a word said
            // differently.
            if (run.Text.Trim().Length == 0 || !run.Text.Any(char.IsLetterOrDigit))
            {
                written.Append(run.Text);
                continue;
            }

            // Emphasis cannot span the space at either end of a run: `** bold**`
            // is not emphasis in any Markdown flavour. So the padding stays
            // outside the marks.
            string trimmed = run.Text.Trim();
            string before = run.Text[..(run.Text.Length - run.Text.TrimStart().Length)];
            string after = run.Text[(run.Text.TrimEnd().Length)..];
            string marks = (run.Bold, run.Italic) switch
            {
                (true, true) => "***",
                (true, false) => "**",
                (false, true) => "*",
                _ => "",
            };

            written.Append(before).Append(marks).Append(trimmed).Append(marks).Append(after);
        }

        return written.ToString();
    }

    /// <summary>The same text with its emphasis taken out.</summary>
    /// <param name="text">Marked-up text.</param>
    public static string Plain(string text) => Emphasis().Replace(text, "$2");

    /// <summary>
    /// A run with the emphasis its neighbours already carry taken off it.
    /// </summary>
    /// <remarks>
    /// The document sets a bold word inside an italic aside, and the run is
    /// both — but the aside is already italic on either side of it, so what the
    /// word adds is the weight alone. Emitting both would close the aside and
    /// reopen it around a word that is not a second aside.
    /// </remarks>
    private static Run Enclosed(List<Run> runs, int at)
    {
        var run = runs[at];
        if (!run.Bold || !run.Italic)
        {
            return run;
        }

        bool before = at > 0 && runs[at - 1].Italic && !runs[at - 1].Bold;
        bool after = at + 1 < runs.Count && runs[at + 1].Italic && !runs[at + 1].Bold;
        return before && after ? run with { Italic = false } : run;
    }

    /// <summary>
    /// Runs set the same way and next to each other, made one.
    /// </summary>
    /// <remarks>
    /// A paragraph is set across several lines and its emphasis does not stop
    /// at the end of one, so an aside spanning three lines arrives as three
    /// italic runs. Emitted separately they become three asides —
    /// <c>*one* *two* *three*</c> — where the page says one.
    /// </remarks>
    /// <param name="runs">The runs, in order.</param>
    public static IEnumerable<Run> Merged(IEnumerable<Run> runs)
    {
        Run? held = null;
        foreach (var run in runs)
        {
            if (held is { } above && above.Bold == run.Bold && above.Italic == run.Italic)
            {
                held = above with { Text = above.Text + run.Text };
                continue;
            }

            if (held is { } previous)
            {
                yield return previous;
            }

            held = run;
        }

        if (held is { } last)
        {
            yield return last;
        }
    }

    /// <summary>
    /// Two lines of a paragraph, joined the way the page reads them.
    /// </summary>
    /// <remarks>
    /// A line break in a justified column is not a space in the text — except
    /// that it usually is. What it is not is a space when the first line ends
    /// mid-word: this document hyphenates, and "Non-" followed by "bolded" is
    /// one word on the page and would be two here.
    /// </remarks>
    /// <param name="above">The paragraph so far.</param>
    /// <param name="below">The next line.</param>
    public static List<Run> Join(List<Run> above, IReadOnlyList<Run> below)
    {
        var right = Trimmed(below, start: true);
        if (above.Count == 0)
        {
            return right;
        }

        var left = Trimmed(above, start: false);
        if (left.Count == 0)
        {
            return right;
        }

        // A hyphen the document set to break a word, which is only a break when
        // what follows it is lower case: "Non-bolded" wraps mid-word, and
        // "per-player" wrapping after the hyphen would too.
        string tail = left[^1].Text;
        bool hyphenated = tail.EndsWith('-')
            && right.Count > 0
            && right[0].Text.Length > 0
            && char.IsLower(right[0].Text[0]);

        if (!hyphenated)
        {
            left[^1] = left[^1] with { Text = tail + " " };
        }

        left.AddRange(right);
        return left;
    }

    private static List<Run> Trimmed(IReadOnlyList<Run> runs, bool start)
    {
        var kept = runs.ToList();
        if (start)
        {
            while (kept.Count > 0 && kept[0].Text.TrimStart().Length == 0) { kept.RemoveAt(0); }

            if (kept.Count > 0) { kept[0] = kept[0] with { Text = kept[0].Text.TrimStart() }; }
        }
        else
        {
            while (kept.Count > 0 && kept[^1].Text.TrimEnd().Length == 0)
            {
                kept.RemoveAt(kept.Count - 1);
            }

            if (kept.Count > 0) { kept[^1] = kept[^1] with { Text = kept[^1].Text.TrimEnd() }; }
        }

        return kept;
    }

    [GeneratedRegex(@"(\*{1,3})(.+?)\1", RegexOptions.Singleline)]
    private static partial Regex Emphasis();
}
