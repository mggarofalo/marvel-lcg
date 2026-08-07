from . import *

# * X-23: Laura Kinney

def GetAbilities() -> Sequence['Ability']:

    def x_23(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "This",
            Enemy,
            x_23,
        ).SetTarget("This", canbe_ready=True),
    ]

