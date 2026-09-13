using System.Text.Json;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One citable unit of the Rules Reference.</summary>
/// <param name="Id">Its citation id — <c>rr:forced.4</c>.</param>
/// <param name="Title">The entry it belongs to, in the document's own casing.</param>
/// <param name="Fragment">The clause, as the index records it for legibility.</param>
/// <param name="Clauses">
/// How many citable records the entry holds, counting itself. Zero on anything
/// that is not an entry.
/// </param>
internal readonly record struct RuleRecord(
    string Id,
    string Title,
    string Fragment,
    string Hash,
    int Clauses,
    string Kind,
    string? BaseId);
