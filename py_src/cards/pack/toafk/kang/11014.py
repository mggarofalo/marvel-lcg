from . import *

# Temporal Shield

def GetAbilities() -> Sequence['Ability']:

    def temporal_shield(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.PreventDamage("All", effect)
        this.DealDamage([message.attacker], 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Kang")
        ),
        AbilityFactory.WhenUnitWouldBeAttacked(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Kang"),
            temporal_shield
        ).LimitOncePerEvent()
        .SetCostFunc(CostFunc.Discard("This"))
    ]

