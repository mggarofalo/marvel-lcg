from . import *

# * Aja-Adanna

def GetAbilities() -> Sequence['Ability']:

    def aja_adanna(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            aja_adanna
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"]),
    ]

