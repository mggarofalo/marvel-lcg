from . import *

# * Thor

def GetAbilities() -> Sequence['Ability']:

    def thor(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(2, effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.Response,
            None,
            "You",
            thor
        ).SetName('"Have at thee!"')
        .SetTarget("Initiator")
        .LimitOncePerPhase(),
    ]

