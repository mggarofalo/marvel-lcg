from . import *

# * Morph: Kevin Sydney

def GetAbilities() -> Sequence['Ability']:

    def morph(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.AfterCardEnterHand(
            AbilityType.Response,
            "This",
            morph
        ).SetTarget(Villain),
    ]

