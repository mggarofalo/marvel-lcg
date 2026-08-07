from . import *

# * Iron Lad: Nathaniel Richards

def GetAbilities() -> Sequence['Ability']:

    def iron_lad(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        allies = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        message.AddThatMatchingPower(allies, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "This",
            iron_lad
        ).SetCostFunc(CostFunc.Exhaust(trait="AVENGER", card_type=Ally)),
    ]

