from . import *

# Savage Strike

def GetAbilities() -> Sequence['Ability']:

    def savage_strike(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(6, effect)
        message.GainPiercing(effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "You",
            savage_strike,
            is_basic_attack=True
        ).SetPlay().SetLabel(),
    ]

