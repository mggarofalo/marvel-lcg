using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>A maximum shared across copies of a card by title.</summary>
public sealed record AbilityMaximum(long Uses, MaximumPeriod Period);
