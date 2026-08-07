from . import *

# * Banshee: Sean Cassidy

def GetAbilities() -> Sequence['Ability']:

    def banshee(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            banshee
        ).SetTarget(Minion, canbe_confused=True),
    ]

