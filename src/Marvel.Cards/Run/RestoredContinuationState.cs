using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;
internal sealed record RestoredContinuationState(
    ImmutableArray<Card> Discarded, ImmutableDictionary<string, long> Results,
    int SourceIncarnation, AbilityContinuationCardBinding? Chosen, Card? Actor,
    ImmutableHashSet<int> CrisisIgnoringThwarts);
