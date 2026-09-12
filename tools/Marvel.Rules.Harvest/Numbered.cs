using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>
/// One step of a numbered procedure, and its lettered sub-steps.
/// </summary>
/// <remarks>
/// Named for the number rather than for the step, because <c>Step</c> is a
/// reserved word in one of the languages the .NET analysers care about.
/// </remarks>
/// <param name="Number">Its position.</param>
/// <param name="Text">The step.</param>
/// <param name="Substeps">The lettered steps under it, if any.</param>
public sealed record Numbered(int Number, string Text, IReadOnlyList<string> Substeps)
{
    /// <summary>This step and its sub-steps, as citable records.</summary>
    /// <param name="entry">The entry's id.</param>
    /// <param name="title">The entry's heading.</param>
    public IEnumerable<RuleRecord> Records(string entry, string title)
    {
        string number = Number.ToString(CultureInfo.InvariantCulture);
        yield return new RuleRecord($"{entry}.step.{number}", [title, $"step {number}"], Text);

        for (int under = 0; under < Substeps.Count; under++)
        {
            char letter = (char)('a' + under);
            yield return new RuleRecord(
                $"{entry}.step.{number}.{letter}",
                [title, $"step {number}", $"step {number}{letter}"],
                Substeps[under]);
        }
    }
}
