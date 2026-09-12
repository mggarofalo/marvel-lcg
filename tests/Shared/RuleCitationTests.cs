using Xunit;

namespace Marvel.Tests;

/// <summary>The citations in this assembly name rules that exist.</summary>
/// <remarks>
/// Linked into each test project, so each one holds its own citations. This is
/// the whole mechanism by which a <see cref="RuleAttribute"/> is a citation
/// rather than a comment: when the Rules Reference is re-harvested and a clause
/// is renumbered or withdrawn, the build says so.
/// </remarks>
public sealed class RuleCitationTests
{
    [Fact]
    public void EveryCitedRuleExists()
    {
        var citations = RuleCitations.In(typeof(RuleCitationTests).Assembly);
        var unknown = citations
            .Where(citation => !RuleCitations.Citable.Contains(citation.Id))
            .Select(citation => $"{citation.Site} cites {citation.Id}")
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"Rules Reference v{RuleCitations.Version} has no such rule:"
            + Environment.NewLine + string.Join(Environment.NewLine, unknown));
    }

    [Fact]
    public void CitationsAreWellFormed()
    {
        // The corpus carries Rules Reference records and audited rules
        // modifications. A test citing `pack:mc11:game-areas` is citing a
        // rules *pack*, which is a different dataset and not covered here --
        // catching that as a typo would be wrong, so it has to be caught as a
        // scheme.
        var citations = RuleCitations.In(typeof(RuleCitationTests).Assembly);
        Assert.DoesNotContain(
            citations, citation => !citation.Id.StartsWith("rr:", StringComparison.Ordinal)
                && !citation.Id.StartsWith("ruling:", StringComparison.Ordinal));
    }
}
