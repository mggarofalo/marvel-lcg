from . import *

# * X-23

def GetAbilities() -> Sequence['Ability']:

    def x_23(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.Response,
            "This",
            x_23
        ).SetName("Living Weapon")
        .SetTarget("This", canbe_ready=True)
        .LimitOncePerPhase(),
    ]

