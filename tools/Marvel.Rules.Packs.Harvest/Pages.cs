using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Packs.Harvest;

public static class Pages
{
    private static readonly string[] HeadingFonts =
        ["Exo2-ExtraBoldItalic", "Exo2-Bold", "ExoMVC-Bold-SC700"];
    private static readonly string[] BodyFonts =
        [
            "Avenir-Book", "Avenir-Black", "Avenir-Heavy", "Avenir-BookOblique",
            "Avenir-Oblique", "Avenir-HeavyOblique", "MarvelLCGIcons",
        ];
    private static readonly string[] BoldFonts = ["Avenir-Black", "Avenir-Heavy"];
    private static readonly string[] ItalicFonts =
        ["Avenir-BookOblique", "Avenir-Oblique", "Avenir-HeavyOblique"];

    public static IReadOnlyList<PackLine> Read(Page page)
    {
        var letters = page.Letters.Where(Content).ToList();
        double? split = ColumnSplit(letters);
        if (split is null)
        {
            return Lines(letters);
        }

        return Lines(letters.Where(letter => letter.GlyphRectangle.Left < split).ToList())
            .Concat(Lines(letters.Where(letter => letter.GlyphRectangle.Left >= split).ToList()))
            .ToList();
    }

    private static bool Content(Letter letter)
    {
        string font = Font(letter);
        double size = letter.PointSize;
        if (size >= 10)
        {
            if (string.Equals(font, "MarvelLCGIcons", StringComparison.Ordinal))
            {
                return size <= 12.5;
            }

            return HeadingFonts.Any(face => font.Contains(face, StringComparison.Ordinal));
        }

        return size >= 7.5 && size <= 9.5
            && BodyFonts.Any(face => font.Contains(face, StringComparison.Ordinal));
    }

    private static double? ColumnSplit(List<Letter> letters)
    {
        if (letters.Count == 0)
        {
            return null;
        }

        const int low = 200;
        const int high = 400;
        var covered = new bool[high - low];
        foreach (Letter letter in letters.Where(letter => !string.IsNullOrWhiteSpace(letter.Value)))
        {
            int start = Math.Max(low, (int)letter.GlyphRectangle.Left);
            int stop = Math.Min(high, (int)letter.GlyphRectangle.Right + 1);
            for (int x = start; x < stop; x++)
            {
                covered[x - low] = true;
            }
        }

        int bestWidth = 0, bestStart = -1, run = 0;
        for (int offset = 0; offset < covered.Length; offset++)
        {
            if (covered[offset])
            {
                run = 0;
                continue;
            }

            run += 1;
            if (run > bestWidth)
            {
                bestWidth = run;
                bestStart = offset - run + 1;
            }
        }

        return bestStart < 0 || bestWidth < 6
            ? null
            : low + bestStart + (bestWidth / 2.0);
    }

    private static List<PackLine> Lines(List<Letter> letters)
    {
        var rows = new List<List<Letter>>();
        List<Letter>? current = null;
        double? baseline = null;
        foreach (Letter letter in letters.OrderByDescending(letter => letter.StartBaseLine.Y))
        {
            if (baseline is null || Math.Abs(letter.StartBaseLine.Y - baseline.Value) <= 3)
            {
                current ??= [];
                current.Add(letter);
                baseline ??= letter.StartBaseLine.Y;
                continue;
            }

            rows.Add(current!);
            current = [letter];
            baseline = letter.StartBaseLine.Y;
        }

        if (current is not null)
        {
            rows.Add(current);
        }

        var lines = new List<PackLine>();
        foreach (var row in rows)
        {
            row.Sort((left, right) => left.GlyphRectangle.Left.CompareTo(right.GlyphRectangle.Left));
            Letter marker = row.FirstOrDefault(letter => !string.IsNullOrWhiteSpace(letter.Value))
                ?? row[0];
            bool heading = IsHeading(marker);
            var spans = new List<Span>();
            foreach (Letter letter in row)
            {
                string text = Decoded(letter.Value);
                string font = Font(letter);
                bool bold = BoldFonts.Any(face => font.Contains(face, StringComparison.Ordinal));
                bool italic = ItalicFonts.Any(face => font.Contains(face, StringComparison.Ordinal));
                if (spans.Count > 0
                    && spans[^1].Bold == bold
                    && spans[^1].Italic == italic)
                {
                    spans[^1] = spans[^1] with { Text = spans[^1].Text + text };
                }
                else
                {
                    spans.Add(new Span(text, bold, italic));
                }
            }

            if (!string.IsNullOrWhiteSpace(string.Concat(spans.Select(span => span.Text))))
            {
                lines.Add(new PackLine(spans, heading));
            }
        }

        return lines;
    }

    private static bool IsHeading(Letter letter)
    {
        string font = Font(letter);
        return letter.PointSize >= 10
            && HeadingFonts.Any(face => font.Contains(face, StringComparison.Ordinal));
    }

    private static string Font(Letter letter)
    {
        string font = letter.FontName ?? string.Empty;
        int subset = font.IndexOf('+', StringComparison.Ordinal);
        return subset >= 0 ? font[(subset + 1)..] : font;
    }

    public static string Decoded(string text) => string.Concat(text.EnumerateRunes().Select(rune =>
    {
        int value = rune.Value;
        bool known = value <= 0x7f
            || value is 0x00ae or 0x00e2 or 0x2018 or 0x2019 or 0x201c or 0x201d
                or 0x2013 or 0x2014 or 0x2026 or 0x2192 or 0xfb01 or 0xfb02
            || value is >= 0xe000 and <= 0xf8ff;
        return known ? rune.ToString() : $"(cid:{value})";
    }));
}
