namespace Marvel.Godot;

/// <summary>A twelve-column grid laid over the table's actual width; every size is a presentation choice.</summary>
/// <remarks>
/// Regions take whole columns, so a wider window gives every region more room instead of scaling a
/// fixed reference canvas. The outer margin and the gutter come from <see cref="SpacingMetrics"/> at
/// the reading scale, so they grow with the text.
/// </remarks>
/// <param name="Width">The table surface's width in pixels.</param>
/// <param name="Margin">The space before the first and after the last column.</param>
/// <param name="Gutter">The space between neighbouring columns.</param>
internal sealed record TableGrid(float Width, float Margin, float Gutter)
{
    /// <summary>The number of columns.</summary>
    internal const int Columns = 12;

    /// <summary>The grid for a table of the given width at a reading scale.</summary>
    internal static TableGrid For(float width, InterfaceScale scale)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        return new TableGrid(width, spacing.Small, spacing.Medium);
    }

    /// <summary>One column's width; never negative.</summary>
    internal float Column => Math.Max(0, (Width - 2 * Margin - (Columns - 1) * Gutter) / Columns);

    /// <summary>The left edge and width of <paramref name="count"/> columns starting at <paramref name="first"/>.</summary>
    internal (float X, float Width) Span(int first, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(first);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first + count, Columns);
        return (Margin + first * (Column + Gutter), count * Column + (count - 1) * Gutter);
    }

    /// <summary>
    /// Divides a span among parts in proportion to their weights, with a gutter between neighbours;
    /// the parts fill the span exactly and keep their order.
    /// </summary>
    internal static (float X, float Width)[] Divide(float x, float width, IReadOnlyList<float> weights, float gutter)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0 || weights.Any(weight => weight <= 0))
            throw new ArgumentException("every part needs a positive weight", nameof(weights));
        float free = Math.Max(0, width - gutter * (weights.Count - 1));
        float total = weights.Sum();
        var parts = new (float X, float Width)[weights.Count];
        float left = x;
        for (int index = 0; index < weights.Count; index++)
        {
            float size = free * weights[index] / total;
            parts[index] = (left, size);
            left += size + gutter;
        }
        return parts;
    }
}
