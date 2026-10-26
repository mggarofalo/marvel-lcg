using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>The authored ability and participants fixed when resolution begins.</summary>
internal sealed record AbilityInvocation(AbilityContinuationAddress Address,
    AbilityContinuationCardBinding Source, Occurrence Occurrence,
    string Trigger, int AbilityPlayer, int AbilityActor)
{
    internal Marvel.Rules.State.Defeated? TriggeringDefeat { get; init; }
    internal Marvel.Rules.State.Attacked? TriggeringAttack { get; init; }
    internal Occurrence? TriggeringThreat { get; init; }
    internal BoostDamageOrigin? DamageOrigin { get; init; }
}
