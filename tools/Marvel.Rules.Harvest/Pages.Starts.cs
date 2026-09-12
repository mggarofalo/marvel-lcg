using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Harvest;

/// <summary>A run of characters set the same way.</summary>
/// <param name="Text">What it says.</param>
/// <param name="Bold">Whether it is set in a heavy weight.</param>
/// <param name="Italic">Whether it is set oblique.</param>

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
