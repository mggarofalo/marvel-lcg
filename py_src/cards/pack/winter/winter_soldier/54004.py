from . import *

# Arm Block

def GetAbilities() -> Sequence['Ability']:

    def arm_block(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

        if YouExhaustedCyberneticArmToPayForThisEvent(effect):
            message.PreventDamage("All", effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            arm_block
        ).SetPlay().SetLabel('attack', 'defense')
        .SetTarget("Attacker"),
    ]

