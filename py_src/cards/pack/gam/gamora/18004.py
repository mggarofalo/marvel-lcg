from . import *

# Crosscounter

def GetAbilities() -> Sequence['Ability']:

    def crosscounter(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventDamage(3, effect)

        this.DealDamage(effect.targets2, 1, effect)
        this.RemoveThreatFromSchemes(effect.targets3, 1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            crosscounter
        ).SetPlay().SetLabel('attack', 'defense', 'thwart')
        .SetTarget2(Enemy)
        .SetTarget3(Scheme2)
    ]

