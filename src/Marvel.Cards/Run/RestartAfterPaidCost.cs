using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;
internal sealed record RestartAfterPaidCost(CompiledCardAbility Ability, AbilityContinuationState State)
    : AbilityContinuationTransition;
