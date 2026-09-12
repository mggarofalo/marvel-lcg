using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Only payment outcomes needed by post-arrow resolution.</summary>
internal readonly record struct AbilityPaymentResult(long? Healed, long? Energy, bool Suspended);
