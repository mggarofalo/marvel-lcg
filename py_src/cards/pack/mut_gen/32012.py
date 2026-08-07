from . import *

# * Polaris: Lorna Dane

def GetAbilities() -> Sequence['Ability']:

    def polaris(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            polaris
        ).SetTarget(Unit2, trait="X-MEN", canbe_tough=True)
    ]

