from . import *

# Problem Solvers

def GetAbilities() -> Sequence['Ability']:

    def problem_solvers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 0
        for unit in effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards:
            if HasThwart.IsType(unit):
                value += unit.thwart

        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        # TODO: Fix this
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            problem_solvers,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2, range="All")
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["AVENGER", "GUARDIAN"])))
    ]

