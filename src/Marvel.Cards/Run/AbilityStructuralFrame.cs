using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// Live frames are typed. Their legacy string encoding is confined to the
// continuation codec at the persistence boundary.
internal abstract record AbilityStructuralFrame;
