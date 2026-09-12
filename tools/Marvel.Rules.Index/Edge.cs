using System.Text.Json;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One authored edge of the rule reference graph.</summary>
/// <param name="From">The rule that names another.</param>
/// <param name="To">What it names.</param>
/// <param name="Why">Why the edge is there, as the dataset records it.</param>
internal readonly record struct Edge(string From, string To, string Why);
