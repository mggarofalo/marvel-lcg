namespace Marvel.Godot;

/// <summary>Partitions measured rows without cutting a choice at a page boundary.</summary>
internal sealed record TableActionPageLayout(IReadOnlyList<TableActionPage> Pages, bool RequiresCompleteChoices)
{
    internal static TableActionPageLayout Measure(IReadOnlyList<float> heights,
        float availableHeight, float navigationHeight, float rowSeparation, float navigationSeparation)
    {
        float total = heights.Sum() + Math.Max(0, heights.Count - 1) * rowSeparation;
        if (total <= availableHeight)
            return new([new(0, heights.Count)], false);

        float pageHeight = availableHeight - navigationHeight - navigationSeparation;
        if (heights.Any(height => height > pageHeight)) return new([], true);
        var pages = new List<TableActionPage>();
        int start = 0;
        float used = 0;
        for (int index = 0; index < heights.Count; index++)
        {
            float next = heights[index] + (index > start ? rowSeparation : 0);
            if (used + next > pageHeight)
            {
                pages.Add(new(start, index - start));
                start = index;
                used = 0;
                next = heights[index];
            }
            used += next;
        }
        pages.Add(new(start, heights.Count - start));
        return new(pages, false);
    }

    internal int ClampPage(int page) => Math.Clamp(page, 0, Math.Max(0, Pages.Count - 1));
}
