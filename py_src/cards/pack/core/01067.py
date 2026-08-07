from . import *

# * Maria Hill

def GetAbilities() -> Sequence['Ability']:

    def maria_hill(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            maria_hill
        ).SetTarget("Players", range="All")
    ]

