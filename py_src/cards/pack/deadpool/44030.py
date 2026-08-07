from . import *

# Stick-To-Itiveness

def GetAbilities() -> Sequence['Ability']:

    def stick_to_itiveness(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            stick_to_itiveness
        ).SetCost(Cost("R"))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHero", canbe_ready=True)
    ]

