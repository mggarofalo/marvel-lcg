from . import *

# Subdue

def GetAbilities() -> Sequence['Ability']:

    def subdue(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(-3, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            subdue
        ).SetPlay().SetLabel()
    ]

