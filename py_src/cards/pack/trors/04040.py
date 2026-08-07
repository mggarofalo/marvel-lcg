from . import *

# * Spider-Girl: Anya Corazon

def GetAbilities() -> Sequence['Ability']:

    def spider_girl(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            spider_girl,
        ).SetTarget(Minion, canbe_status=["Stunned", "Confused"]),
    ]

