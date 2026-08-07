from . import *

# * Dazzler: Alison Blaire

def GetAbilities() -> Sequence['Ability']:

    def dazzler(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            dazzler,
        ).SetTarget(Enemy, canbe_confused=True),
    ]

