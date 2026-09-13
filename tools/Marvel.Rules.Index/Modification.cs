using System.Text.Json;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>A published ruling layered over one citable Rules Reference record.</summary>
internal readonly record struct Modification(
    string Id,
    string BaseId,
    string SupersedesHash,
    string? AbsorbedIn,
    string Why,
    string Source,
    string Via,
    string Scope,
    string? Observed,
    string Hash);
