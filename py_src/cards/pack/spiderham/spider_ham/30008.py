from . import *

# * The Daily Beagle

def GetAbilities() -> Sequence['Ability']:

    def the_daily_beagle(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'toon', effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            the_daily_beagle
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Peter Porker"),
    ]

