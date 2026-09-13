using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Typed facts reconstructed at the legacy continuation boundary. These carry
// authored nodes and cursor values, not the string wire that encoded them.
internal abstract record AbilityContinuationFrame;
