from . import *

# Avengers Mansion

def GetAbilities() -> Sequence['Ability']:

    def avengers_mansion(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Players.DrawUp(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            avengers_mansion
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Players")
        # .SetDesc("Exhaust Avengers Mansion → choose a player. That player draws 1 card")
    ]

