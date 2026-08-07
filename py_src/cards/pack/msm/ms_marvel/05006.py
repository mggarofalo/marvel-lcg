from . import *

# * Aamir Khan

def GetAbilities() -> Sequence['Ability']:

    def aamir_khan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.MoveAllToDeck(effect.targets, initiator.player_deck, "Bottom", effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            aamir_khan
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(from_where=["YourDiscardPile"]),
    ]

