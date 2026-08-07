from . import *

# * Two-Gun Kid: Matthew Hawk

def GetAbilities() -> Sequence['Ability']:

    def two_gun_kid(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.AddTarget(*effect.targets)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "This",
            two_gun_kid,
            is_basic_attack=True
        ).SetTarget(Enemy),
    ]

