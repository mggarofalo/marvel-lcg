from . import *

# * Safe House #29

def GetAbilities() -> Sequence['Ability']:

    def safe_house_29(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            safe_house_29
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(trait="PREPARATION", from_where=["YourDiscardPile"]),
    ]

