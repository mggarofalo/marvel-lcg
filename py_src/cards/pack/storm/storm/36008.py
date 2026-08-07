from . import *

# * Ororo's Garden

def GetAbilities() -> Sequence['Ability']:

    def ororos_garden(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            ororos_garden
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourIdentity", canbe_heal=True),
    ]

