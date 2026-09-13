using System.Text.Json;
using Marvel.Rules.Index;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

/// <summary>One canonical authority unit before behavioral adjudication.</summary>
internal sealed record AuthoritySource(
    string Id,
    string Kind,
    string Title,
    string Fingerprint,
    string Scope,
    string Text);
