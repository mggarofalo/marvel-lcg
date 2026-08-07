from . import *

# * The Golden City

def GetAbilities() -> Sequence['Ability']:

    def the_golden_city(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            the_golden_city
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

