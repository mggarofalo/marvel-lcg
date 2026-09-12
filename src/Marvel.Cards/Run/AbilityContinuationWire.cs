using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

internal sealed record AbilityContinuationWire(
    int Ordinal, ImmutableArray<string> Path, ImmutableArray<int> ActivationIds,
    ImmutableDictionary<string, long> Results, string Face, int Player, int Actor,
    Occurrence? Occurrence, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, ImmutableArray<int> Discarded);
