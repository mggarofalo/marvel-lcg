using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Harvest;

/// <summary>A run of characters set the same way.</summary>
/// <param name="Text">What it says.</param>
/// <param name="Bold">Whether it is set in a heavy weight.</param>
/// <param name="Italic">Whether it is set oblique.</param>
public readonly record struct Run(string Text, bool Bold, bool Italic);

/// <summary>What a line starts.</summary>
public enum Starts
{
    /// <summary>More of whatever came before it.</summary>
    More,

    /// <summary>An entry, or a section of the document.</summary>
    Heading,

    /// <summary>A top-level clause — the document sets these with a bullet.</summary>
    Clause,

    /// <summary>A qualification of the clause above it.</summary>
    SubClause,

    /// <summary>The entry's cross-references.</summary>
    SeeAlso,

    /// <summary>
    /// One step of a numbered procedure — <c>rr:villain-phase</c>'s six, the
    /// nine <c>rr:damage</c> lists.
    /// </summary>
    /// <remarks>
    /// A different thing from a clause, and the Rules Reference cites it that
    /// way itself: "during step three of the villain phase" is its own
    /// phrasing, in three separate entries.
    /// </remarks>
    Step,

    /// <summary>
    /// A lettered step under a numbered one — <c>rr:ability.step.2.a</c>.
    /// </summary>
    /// <remarks>
    /// The Simultaneous Timing Priority chart is the reason these are their own
    /// grain: without them it is a run-on sentence inside one clause.
    /// </remarks>
    SubStep,
}

/// <summary>One line of a page, in reading order.</summary>
/// <param name="Runs">The line, split where its setting changes.</param>
/// <param name="Left">Where the line starts, in points from the page's left edge.</param>
/// <param name="Size">The tallest glyph on the line, which is what separates a heading from prose.</param>
/// <param name="Kind">What the line starts.</param>
public readonly record struct Line(
    IReadOnlyList<Run> Runs, double Left, double Size, Starts Kind)
{
    /// <summary>The line with its setting thrown away.</summary>
    public string Text => string.Concat(Runs.Select(run => run.Text));

    /// <summary>The glyph the line opens with, where that glyph is a marker.</summary>
    public Marker Marker { get; init; }

    /// <summary>Whether the line is set in one of the document's heading faces.</summary>
    public bool Titled { get; init; }
}

/// <summary>The list marker a line opens with.</summary>
public enum Marker
{
    /// <summary>None — the line is prose, or a wrapped continuation.</summary>
    None,

    /// <summary>A bullet, which the document sets in its heavy weight.</summary>
    Bullet,

    /// <summary>A second-level marker, set in a font that carries no glyph for it.</summary>
    Dash,
}

/// <summary>
/// Turning a two-column page into lines in the order a reader reads them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The gutter is found, not measured.</b> This document sets recto and verso
/// with different margins, so the empty band between its two columns is at
/// roughly 291–308pt on one and 303–321pt on the other. A single measured split
/// is correct for the first layout and lands <i>inside the left column's text</i>
/// on the second — where it truncates the last character of any line that runs
/// long, and files it under the other column. So the split is found per page as
/// the widest character-free band, and a page whose widest band cuts through a
/// glyph is refused: a split that bisects a character is not a gutter.
/// </para>
/// <para>
/// <b>Headings are struck twice.</b> The document draws its entry titles with
/// the same glyphs overprinted a fraction of a point apart, so PdfPig reads
/// <c>OOVVEERRVVIIEEWW</c> where the page says <c>OVERVIEW</c>. That is a
/// property of how the text was set rather than of the words, and it is undone
/// here rather than anywhere downstream — every later stage would otherwise
/// have to know about it.
/// </para>
/// </remarks>
public static partial class Pages
{
    /// <summary>Every line of a page, left column then right.</summary>
    /// <param name="page">The page.</param>
    public static IReadOnlyList<Line> Read(Page page)
    {
        double gutter = Gutter(page);
        var lines = new List<Line>();
        lines.AddRange(Classified(Column(page, 0, gutter)));
        lines.AddRange(Classified(Column(page, gutter, page.Width)));
        return lines;
    }

    /// <summary>
    /// The widest vertical band of the page no glyph occupies.
    /// </summary>
    /// <remarks>
    /// Searched only across the middle half of the page, because the margins are
    /// wider than the gutter and would win. Answering the band's centre rather
    /// than an edge keeps the split away from both columns when the band is
    /// wide.
    /// </remarks>
    /// <param name="page">The page.</param>
    public static double Gutter(Page page)
    {
        double from = page.Width * 0.25;
        double to = page.Width * 0.75;

        // One bucket per point, marked where any glyph covers it. A glyph
        // spanning the split is what makes a band not a gutter, so this is
        // coverage rather than a count of starts.
        var covered = new bool[(int)Math.Ceiling(page.Width) + 1];
        foreach (var letter in page.Letters)
        {
            int left = (int)Math.Floor(letter.GlyphRectangle.Left);
            int right = (int)Math.Ceiling(letter.GlyphRectangle.Right);
            for (int x = Math.Max(0, left); x <= Math.Min(covered.Length - 1, right); x++)
            {
                covered[x] = true;
            }
        }

        int bestStart = -1, bestWidth = 0, start = -1;
        for (int x = (int)from; x <= (int)to; x++)
        {
            if (!covered[x])
            {
                start = start < 0 ? x : start;
                if (x - start + 1 > bestWidth)
                {
                    bestWidth = x - start + 1;
                    bestStart = start;
                }
            }
            else
            {
                start = -1;
            }
        }

        return bestStart < 0
            ? page.Width / 2
            : bestStart + (bestWidth / 2.0);
    }

    /// <summary>
    /// The faces the document uses for anything that is not its text.
    /// </summary>
    /// <remarks>
    /// <c>Exo2-Regular-SC850</c> is the running head, set sideways up the outer
    /// edge. <c>KomikaTitle</c>, <c>Futura</c>, <c>Elektra</c> and
    /// <c>TimesNewRoman</c> are card art — the document reproduces cards, and a
    /// card has text on it. <c>MyriadPro</c> carries the second-level bullet,
    /// which is a marker rather than a word.
    /// </remarks>
    private static readonly string[] Furniture =
    [
        "Exo2-Regular", "KomikaTitle", "Futura", "Elektra", "TimesNewRoman",
        "Arial", "MyriadPro",
    ];

    internal const string Closing = ")]}”’,.;:!?";

    internal const string Opening = "([{“‘";

    /// <summary>The glyph legend, as <c>icons.json</c> records it.</summary>
    /// <remarks>
    /// The document sets its icons in a font of its own, at codepoints in
    /// Unicode's private use area — so what a reader sees as a lightning bolt
    /// is <c>U+F528</c> and means nothing to anything that does not have the
    /// font. The legend is what turns it back into a word.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> Icons { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// What each line of a column starts, from where it sits and how it is set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>By marker, not by indent.</b> This document sets recto and verso with
    /// different margins, so the same clause bullet is at 65.9pt on one page
    /// and 78.8pt on the next — and a clause's wrapped second line sits exactly
    /// where a sub-clause hangs its marker. What each line starts is legible in
    /// the glyph it opens with and not in where it sits.
    /// </para>
    /// <para>
    /// <b>A sub-clause's marker is a glyph its font does not carry.</b> The
    /// document sets the second-level bullet in MyriadPro and what comes out is
    /// <c>U+0000</c> on one page and <c>U+0020</c> on another — so the
    /// character is no use and the <i>font</i> is, because MyriadPro is used
    /// nowhere else in the document. Indent alone cannot serve: a sub-clause
    /// hangs its marker at the same place a clause wraps its second line to.
    /// </para>
    /// </remarks>
    private static IEnumerable<Line> Classified(IEnumerable<Line> column)
    {
        foreach (var line in column)
        {
            var kind = Starts.More;

            if (line.Size >= 8 && line.Titled)
            {
                kind = Starts.Heading;
            }
            else if (line.Marker == Marker.Bullet)
            {
                kind = Starts.Clause;
            }
            else if (line.Marker == Marker.Dash)
            {
                kind = Starts.SubClause;
            }
            else if (line.Runs.Count > 0
                     && line.Runs[0].Bold
                     && line.Runs[0].Text.StartsWith("See also", StringComparison.Ordinal))
            {
                kind = Starts.SeeAlso;
            }
            else if (Numbered(line) is not null)
            {
                kind = Starts.Step;
            }
            else if (Lettered(line) is not null)
            {
                kind = Starts.SubStep;
            }

            yield return line with { Kind = kind };
        }
    }

    private static IEnumerable<Line> Column(Page page, double from, double to)
    {
        // The page's own furniture -- a running head and a folio -- sits
        // outside the text block and is not part of any entry.
        bool Inside(double bottom) => bottom > 40 && bottom < 730;

        // The running head is set sideways up the outer edge, so its glyphs sit
        // inside the text block's vertical span and are not part of it.
        static bool Upright(Letter letter) =>
            letter.TextOrientation == TextOrientation.Horizontal;

        // **The document reproduces cards, and a card has text on it.** Rhino's
        // boost icons, an event's "Hero Action:" line, the copyright under the
        // art -- all of it sits on the same baselines as the prose beside it and
        // reads as part of it. So does the running head. What separates them is
        // the face, and it is written as a list of what to leave out rather
        // than what to keep: the text is set in five faces and a new edition
        // adding a sixth should read as text, while these seven are furniture
        // in any edition.
        static bool Body(Letter letter)
        {
            string font = letter.FontName ?? string.Empty;
            return !Furniture.Any(face => font.Contains(face, StringComparison.Ordinal));
        }

        var words = page.GetWords()
            .Where(word => word.BoundingBox.Left >= from && word.BoundingBox.Left < to)
            .Where(word => Inside(word.BoundingBox.Bottom))
            .Where(word => word.Letters.All(Upright))
            .Where(word => word.Letters.Any(Body))
            .ToList();

        // Kept beside the words because a marker glyph is not in one: the
        // document's second-level bullet is set in a font that carries no glyph
        // for it, so what is on the page is a space, and a space is not a word.
        // Every glyph of every line, kept beside the words: a heading's spacing
        // has to be measured from the glyphs, and a marker is not in a word.
        var raw = page.Letters
            .Where(letter => letter.GlyphRectangle.Left >= from
                             && letter.GlyphRectangle.Left < to)
            .Where(letter => Inside(letter.StartBaseLine.Y) && Upright(letter))
            .GroupBy(letter => Math.Round(letter.StartBaseLine.Y, 0))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(letter => letter.GlyphRectangle.Left).ToList());

        var marks = page.Letters
            .Where(letter => letter.GlyphRectangle.Left >= from
                             && letter.GlyphRectangle.Left < to)
            .Where(letter => Inside(letter.StartBaseLine.Y) && Upright(letter))
            .GroupBy(letter => Math.Round(letter.StartBaseLine.Y, 0))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(letter => letter.GlyphRectangle.Left).First());

        // Grouped by baseline. A superscript or a differently-sized glyph moves
        // the baseline by a fraction of a point, so this rounds rather than
        // comparing: two words a tenth of a point apart are on one line.
        foreach (var group in words
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 0))
            .OrderByDescending(group => group.Key))
        {
            var ordered = group.OrderBy(word => word.BoundingBox.Left).ToList();
            var runs = Runs(ordered);
            if (runs.Count == 0)
            {
                continue;
            }

            var first = marks.TryGetValue(group.Key, out var mark)
                ? mark
                : ordered[0].Letters[0];

            // A heading is letterspaced and carries no space glyphs at all --
            // "THEGOLDENRULES" is what is in the file -- so where its words
            // divide is a measurement rather than something the document says.
            // PdfPig's segmentation is tuned for body text and splits ABILITY
            // after the T, so a heading gets its own.
            var glyphs = raw.TryGetValue(group.Key, out var all) ? all : [];
            if (glyphs.Count > 0
                && glyphs.Max(letter => letter.GlyphRectangle.Height) >= 8
                && Titling(glyphs))
            {
                runs = [new Run(Struck(Spaced(glyphs)), true, false)];
            }

            yield return new Line(
                runs,
                first.GlyphRectangle.Left,
                ordered.SelectMany(word => word.Letters)
                       .Max(letter => letter.GlyphRectangle.Height),
                Starts.More)
            {
                Marker = first.Value == "\u2022" ? Marker.Bullet
                    : (first.FontName ?? "").Contains("MyriadPro", StringComparison.Ordinal)
                        ? Marker.Dash
                        : Marker.None,
                Titled = glyphs.Count > 0 && Titling(glyphs),
            };
        }
    }

    /// <summary>
    /// One line, gathered into runs of a single setting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Words for the spacing, letters for the setting.</b> A PDF positions
    /// each glyph and says nothing about the gaps between them, so where a word
    /// ends has to be inferred — and this font kerns widely enough that a fixed
    /// fraction of the point size splits "confuse" into "con fuse". PdfPig's
    /// own segmentation is what decides that; what it cannot decide is weight
    /// and slope, which vary <i>inside</i> a word.
    /// </para>
    /// <para>
    /// <b>Weight and slope come from the font name</b>, because this document
    /// sets bold and italic as separate embedded fonts rather than as a style
    /// on one. Avenir's family names carry both:
    /// <c>Avenir-Book</c>, <c>-BookOblique</c>, <c>-Black</c>, <c>-Heavy</c>,
    /// <c>-HeavyOblique</c>.
    /// </para>
    /// </remarks>
    private static List<Run> Runs(List<Word> words)
    {
        var runs = new List<Run>();
        var text = new StringBuilder();
        bool bold = false, italic = false;

        for (int at = 0; at < words.Count; at++)
        {
            // **A word break is not always a space.** PdfPig measures the gap
            // between glyphs, and an icon or a curly quote is wider than the
            // letter beside it -- so "([per-player])" comes back as three
            // words and "Only”" as two. Punctuation that closes something
            // never takes a space before it, and punctuation that opens one
            // never takes a space after.
            if (at > 0 && !Closes(words[at]) && !Opens(words[at - 1]))
            {
                text.Append(' ');
            }

            foreach (var letter in words[at].Letters)
            {
                bool nowBold = Heavy(letter.FontName);
                bool nowItalic = Oblique(letter.FontName);

                if (text.Length > 0 && (nowBold != bold || nowItalic != italic))
                {
                    runs.Add(new Run(text.ToString(), bold, italic));
                    text.Clear();
                }

                bold = nowBold;
                italic = nowItalic;
                text.Append(Glyph(letter));
            }
        }

        if (text.Length > 0)
        {
            runs.Add(new Run(text.ToString(), bold, italic));
        }

        // Undoubling is per word and the doubling is per line, so it happens
        // once the runs are assembled rather than glyph by glyph.
        return
        [
            .. runs
                .Select(run => run with { Text = Struck(run.Text) })
                .Where(run => run.Text.Length > 0),
        ];
    }

    /// <summary>The number a line opens with, if it opens a numbered step.</summary>
    /// <remarks>
    /// The document sets a step's number in its heavy weight and follows it
    /// with a stop, which is what separates "1." starting a step from a
    /// sentence that happens to begin with a figure.
    /// </remarks>
    /// <param name="line">A line.</param>
    public static int? Numbered(Line line)
    {
        if (line.Runs.Count == 0 || !line.Runs[0].Bold)
        {
            return null;
        }

        // The document sets a step two ways: "**1. **Player phase begins." in
        // the round overview, and "**1. Give boost card: **If a villain..." in
        // the attack, where the number, the step's name and the colon are one
        // bold run. Both start with the figure and a stop.
        var found = Figure().Match(line.Runs[0].Text.TrimStart());
        return found.Success
            ? int.Parse(found.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
            : null;
    }

    [GeneratedRegex(@"^(\d+)\.")]
    private static partial Regex Figure();

    /// <summary>Whether a line is set in one of the document's heading faces.</summary>
    /// <remarks>
    /// The size alone will not do: the document reproduces card images, and a
    /// card's title is set as large as an entry's. The heading faces are used
    /// nowhere but headings.
    /// </remarks>
    private static bool Titling(List<Letter> glyphs) =>
        glyphs.Count(letter =>
            (letter.FontName ?? "").Contains("ExoMVC", StringComparison.Ordinal))
        > glyphs.Count / 2;

    /// <summary>
    /// A heading's glyphs, with a space wherever they are set far enough apart.
    /// </summary>
    /// <remarks>
    /// <b>0.36 of the glyph height</b>, swept against the 262 titles the
    /// document holds: everything from 0.32 up to 0.44 gives the same answer,
    /// and the middle of a plateau is the least likely place for the next
    /// document to fall off it.
    /// </remarks>
    private static string Spaced(List<Letter> glyphs)
    {
        const double Share = 0.36;
        var built = new StringBuilder();
        for (int at = 0; at < glyphs.Count; at++)
        {
            string glyph = Glyph(glyphs[at]);
            if (at > 0
                && glyphs[at].GlyphRectangle.Left - glyphs[at - 1].GlyphRectangle.Right
                   > glyphs[at].GlyphRectangle.Height * Share
                && !(glyph.Length > 0 && Closing.Contains(glyph[0]))
                && !Opening.Contains(built.Length > 0 ? built[^1] : ' '))
            {
                built.Append(' ');
            }

            built.Append(glyph);
        }

        // A heading's spacing is measured, and some of its glyphs are spaces
        // already -- so a wide gap either side of one would put in a second.
        return Whitespace().Replace(built.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>The letter a line opens with, if it opens a lettered sub-step.</summary>
    /// <param name="line">A line.</param>
    public static char? Lettered(Line line)
    {
        if (line.Runs.Count == 0 || !line.Runs[0].Bold)
        {
            return null;
        }

        string head = line.Runs[0].Text.TrimStart();
        return head.Length >= 2 && head[1] == '.' && char.IsAsciiLetterLower(head[0])
            ? head[0]
            : null;
    }

    // `)`, `”`, `,` and their kind. A word that begins with one of these is
    // finishing what came before it.
    private static bool Closes(Word word) =>
        word.Text.Length > 0 && Closing.Contains(word.Text[0]);

    // `(` and `“`. A word that ends with one of these is opening what follows.
    private static bool Opens(Word word) =>
        word.Text.Length > 0 && Opening.Contains(word.Text[^1]);

    private static string Glyph(Letter letter)
    {
        if (letter.Value.Length == 1
            && letter.Value[0] >= '\uE000'
            && Icons.TryGetValue(letter.Value, out string? named))
        {
            return $"[{named}]";
        }

        return letter.Value;
    }

    private static bool Heavy(string? font) =>
        font is not null
        && (font.Contains("Black", StringComparison.Ordinal)
            || font.Contains("Heavy", StringComparison.Ordinal)
            || font.Contains("Bold", StringComparison.Ordinal));

    private static bool Oblique(string? font) =>
        font is not null
        && (font.Contains("Oblique", StringComparison.Ordinal)
            || font.Contains("Italic", StringComparison.Ordinal));

    /// <summary>
    /// Undoes an overprinted heading — <c>OOVVEERRVVIIEEWW</c> to <c>OVERVIEW</c>.
    /// </summary>
    /// <remarks>
    /// Only when <b>every</b> character is doubled, which is what makes this
    /// safe: a word with a real double letter in it — <c>ALL</c>, <c>OFF</c> —
    /// has an odd run somewhere and is left alone. A word of one repeated
    /// letter is the case that cannot be told apart, and the document prints
    /// none.
    /// </remarks>
    /// <param name="text">A line as the words read.</param>
    public static string Struck(string text)
    {
        return string.Join(' ', text.Split(' ').Select(Undouble));

        static string Undouble(string word)
        {
            if (word.Length < 2 || word.Length % 2 != 0)
            {
                return word;
            }

            for (int at = 0; at < word.Length; at += 2)
            {
                if (word[at] != word[at + 1])
                {
                    return word;
                }
            }

            var halved = new char[word.Length / 2];
            for (int at = 0; at < halved.Length; at++)
            {
                halved[at] = word[at * 2];
            }

            return new string(halved);
        }
    }
}
