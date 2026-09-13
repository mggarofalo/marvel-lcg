namespace Marvel.Tests;

/// <summary>Where a <see cref="RuleAttribute"/> sits, and what it cites.</summary>
/// <param name="Id">The cited id.</param>
/// <param name="Site">The type or method carrying the citation, for a failure message.</param>
internal readonly record struct Citation(string Id, string Site);
