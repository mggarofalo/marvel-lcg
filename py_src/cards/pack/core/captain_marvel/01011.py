from . import *

# * Spider-Woman

def GetAbilities() -> Sequence['Ability']:

    def spider_woman(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            spider_woman
        ).SetTarget(Villain, canbe_confused=True)
    ]

