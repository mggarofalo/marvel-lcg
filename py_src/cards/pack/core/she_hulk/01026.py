from . import *

# Superhuman Law Division

def GetAbilities() -> Sequence['Ability']:

    def superhuman_law_division(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            superhuman_law_division
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("B"))
        .SetTarget(Scheme2),
    ]

