using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;
internal sealed record ScheduleActivationsCommand(
    AbilityEffect.ActivateEnemies Effect, ImmutableArray<ActivationTarget> Targets,
    int Against, bool First, bool Dynamic) : AbilityStructuralTransition;
