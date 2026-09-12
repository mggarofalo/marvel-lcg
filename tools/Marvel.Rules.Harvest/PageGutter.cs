using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Harvest;

/// <summary>Finds the glyph-free gutter between a Rules Reference page's columns.</summary>
internal static class PageGutter
{
    internal static double Find(Page page)
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
}
