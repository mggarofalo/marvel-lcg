from . import *

# * Nova

def GetAbilities() -> Sequence['Ability']:

    def nova(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            nova,
            powers=["ATK", "THW", "DEF"]
        ).SetTarget(name="Supernova Helmet", canbe_ready=True),
    ]

