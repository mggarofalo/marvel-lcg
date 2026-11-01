using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Binds the identity dealing excess damage, and each enemy dealt it, as trigger instances.</summary>
/// <remarks>
/// The actor of an excess-damage condition is the identity credited with the damage
/// (rr:you-your.12-.14), not the card that dealt it. Each enemy dealt excess damage
/// is its own instance (rr:triggering-condition.1; rulings 3f57fe6e7dc99dd2 and
/// 80e8d8516006df27), sharing one response window (rr:triggering-condition.2).
/// </remarks>
internal static class AbilityExcessDamageInstances
{
    internal static bool Applies(AbilityTrigger trigger, Occurrence occurrence) =>
        Names(trigger, ExcessDamageTiming.ExcessDamageDealt)
        || Names(trigger, ExcessDamageTiming.AttackExcessDamageWouldBeDealt) && occurrence.Damage is not null;

    internal static IEnumerable<AbilityWindowAdmission.Candidate> For(
        AbilityWindowAdmission.Candidate candidate, Occurrence occurrence)
    {
        if (Names(candidate.Ability.Trigger, ExcessDamageTiming.AttackExcessDamageWouldBeDealt))
        {
            if (occurrence.Damage?.ExcessDealer is { } dealer)
                yield return candidate with { TriggeringActor = dealer };
            yield break;
        }
        foreach (var dealt in occurrence.DealtEnemyDamage.Where(dealt => dealt.Excess > 0))
            yield return candidate with
            {
                TriggeringActor = dealt.Dealer,
                TriggeringAttack = dealt.Recipient with { ExcessDamage = dealt.Excess },
            };
    }

    private static bool Names(AbilityTrigger trigger, string condition) =>
        trigger.Event == condition || trigger.Also == condition;
}
