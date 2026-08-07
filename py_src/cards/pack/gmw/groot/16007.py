from . import *

# Fertile Ground

def GetAbilities() -> Sequence['Ability']:

    def fertile_ground(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.PlaceCountersOn(effect.targets, 1, 'growth', effect, maximum=10)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            fertile_ground
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Groot"),
    ]

