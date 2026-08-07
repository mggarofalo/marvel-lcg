from . import *

# * Quicksilver

def GetAbilities() -> Sequence['Ability']:

    def quicksilver(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            quicksilver,
            powers=["ATK", "THW", "DEF"],
        ).SetTarget("YourHero")
        .LimitOncePerPhase()
        .SetName("Super Speed")
    ]

