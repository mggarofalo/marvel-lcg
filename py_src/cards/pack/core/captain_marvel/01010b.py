from . import *

# * Carol Danvers

def GetAbilities() -> Sequence['Ability']:

    def carol_danvers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        for target in effect.targets:
            target.GetControlByPlayer().DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            carol_danvers
        ).SetTarget("Players")
        .SetName("Commander")
        .LimitOncePerRound()
    ]

