from . import *

# * White Tiger: Ava Ayala

def GetAbilities() -> Sequence['Ability']:

    def white_tiger(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = Worlds.GetVillainStage(effect)
        value = Math.MinMax(value, 1, 3)
        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(value, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            white_tiger,
        ).SetTarget("Initiator"),
    ]

