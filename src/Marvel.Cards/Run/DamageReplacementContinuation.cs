using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resumes mandatory replacement ordering after an individual ability completes.</summary>
internal sealed record DamageReplacementContinuation(DamageReplacement Replacement) : CardAbilityContinuation;
