namespace Marvel.Rules.Play;

/// <summary>A read-only projection of damage after forced step-1 replacements.</summary>
/// <param name="Result">Known amounts, possible amounts, or a missing calculation.</param>
/// <param name="Note">A render-safe explanation of the reached replacement.</param>
public sealed record DamageProjection(RuleProjection<long> Result, string? Note = null);
