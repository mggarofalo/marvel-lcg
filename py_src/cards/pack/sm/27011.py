from . import *

# * Spider-Man: Miles Morales

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            spider_man,
            conditions=[
                lambda effect, message:
                    len(effect.GetInitiator().GetControlCards2(CardFinder2("WEB-WARRIOR"))) >= 3
            ]
        ).SetTarget(Enemy, canbe_status=["Stunned", "Confused"]),
    ]

