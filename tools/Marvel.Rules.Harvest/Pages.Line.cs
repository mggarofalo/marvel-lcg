using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Harvest;

/// <summary>A run of characters set the same way.</summary>
/// <param name="Text">What it says.</param>
/// <param name="Bold">Whether it is set in a heavy weight.</param>
/// <param name="Italic">Whether it is set oblique.</param>

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
