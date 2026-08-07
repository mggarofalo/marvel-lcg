from . import *

# * Logan's Cabin

def GetAbilities() -> Sequence['Ability']:

    def logans_cabin(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            logans_cabin
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"])
    ]

