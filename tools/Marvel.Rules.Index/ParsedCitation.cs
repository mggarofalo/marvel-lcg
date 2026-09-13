using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One citation as it appears in a parsed source configuration.</summary>
/// <param name="Position">The attribute's source position.</param>
/// <param name="Id">The cited id.</param>
internal readonly record struct ParsedCitation(int Position, string Id);
