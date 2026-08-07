from . import *

# Helicarrier

def GetAbilities() -> Sequence['Ability']:

    def helicarrier(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        for target in effect.targets:
            player = target.GetControlByPlayer()
            Worlds.UpdateNextCardPlayCost(
                player,
                -1,
                effect,
                in_this="Phase"
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            helicarrier
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Players")
    ]
