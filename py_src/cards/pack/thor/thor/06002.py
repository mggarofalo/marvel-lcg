from . import *

# * Lady Sif

def GetAbilities() -> Sequence['Ability']:

    def lady_sif(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            lady_sif
        ).SetTarget(names=["Thor", "Odinson"], canbe_ready=True),
    ]

