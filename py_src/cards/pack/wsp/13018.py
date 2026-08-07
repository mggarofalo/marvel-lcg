from . import *

# * Ironheart

def GetAbilities() -> Sequence['Ability']:

    def ironheart(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)

    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            ironheart,
        ).SetTarget("Initiator")
    ]

