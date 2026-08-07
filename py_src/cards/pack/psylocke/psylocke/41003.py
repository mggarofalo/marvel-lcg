from . import *

# * Angel: Warren Worthington III

def GetAbilities() -> Sequence['Ability']:

    def angel(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            "This",
            angel
        ).SetTarget("YourIdentity", canbe_ready=True),
    ]

