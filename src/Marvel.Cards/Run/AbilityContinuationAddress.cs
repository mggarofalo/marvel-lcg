using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Continuation wire data is intentionally decoded against the compiled program.
// Paths are engine-chosen save data, not an alternate executable syntax.
internal sealed record AbilityContinuationAddress(string Face, AbilityType? Tier, int Ordinal);
