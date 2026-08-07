from . import *

# Strength In Numbers

def GetAbilities() -> Sequence['Ability']:

    def strength_in_numbers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        size = len(effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards)
        initiator.DrawUp(size, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            strength_in_numbers
        ).SetPlay()
        .SetCostFunc(CostFunc.Exhaust("YourAlly",
            range=(1, "All")))
    ]

