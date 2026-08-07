from . import *

# Powerful Punch

def GetAbilities() -> Sequence['Ability']:

    def powerful_punch(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            powerful_punch,
        ).SetPlay().SetLabel('attack', 'defense')
        .SetTarget("Trigger"),
    ]

