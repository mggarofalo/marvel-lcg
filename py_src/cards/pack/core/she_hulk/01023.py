from . import *

# Legal Practice

def GetAbilities() -> Sequence['Ability']:

    def legal_practice(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        size = len(effect.cost_func.Get(CostFunc.Discard).return_discarded_cards)
        this.RemoveThreatFromSchemes(effect.targets, size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            legal_practice
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.Discard("YourHandCards", range=(1, 5)))
        .SetTarget(Scheme2),
    ]

