using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

namespace Marvel.Rules.Harvest;

/// <summary>A run of characters set the same way.</summary>
/// <param name="Text">What it says.</param>
/// <param name="Bold">Whether it is set in a heavy weight.</param>
/// <param name="Italic">Whether it is set oblique.</param>
public readonly record struct Run(string Text, bool Bold, bool Italic);
