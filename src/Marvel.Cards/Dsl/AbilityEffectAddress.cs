using System.Collections.Immutable;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

/// <summary>A deterministic address of an effect in one authored ability.</summary>
/// <remarks>
/// Paths use explicit DSL field names and ordered list indexes, never CLR type
/// names. This internal lookup does not change the session-ledger wire format.
/// </remarks>
public sealed record AbilityEffectAddress(string Card, int Ability, string Path);
