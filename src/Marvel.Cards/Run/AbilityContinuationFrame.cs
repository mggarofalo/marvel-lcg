using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Query facts derived from the game-owned structural frames.
internal abstract record AbilityContinuationFrame;
