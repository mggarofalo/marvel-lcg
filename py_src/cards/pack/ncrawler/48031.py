from . import *

# Combine Forces

def GetAbilities() -> Sequence['Ability']:

    def combine_forces(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.DefeatUnits(effect.targets, this, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            combine_forces
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["X-FORCE", "X-MEN"])))
        .SetTarget(Minion, non_trait="ELITE"),
    ]

