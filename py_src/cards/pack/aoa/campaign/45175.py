from . import *

# * X-Man: Nate Grey

def GetAbilities() -> Sequence['Ability']:

    def x_man(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterCardEnterHand(
            AbilityType.Response,
            "This",
            x_man
        ).SetTarget("YourIdentity", canbe_tough=True),
    ]

