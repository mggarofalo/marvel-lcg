from . import *

# * Serval Industries

def GetAbilities() -> Sequence['Ability']:

    def serval_industries(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            serval_industries
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(card_class="IdentitySpecific", range=(2, 2), from_where=["YourDiscardPile"])
    ]

