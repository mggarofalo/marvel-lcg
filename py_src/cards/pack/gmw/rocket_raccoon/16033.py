from . import *

# Salvage

def GetAbilities() -> Sequence['Ability']:

    def salvage(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.MoveAllToDeck(effect.targets, initiator.player_deck, "Top", effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.Response,
            salvage
        ).SetTarget(trait="TECH", from_where=["YourDiscardPile"]),
    ]

