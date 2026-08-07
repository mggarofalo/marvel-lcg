from . import *

# * Wasp

def GetAbilities() -> Sequence['Ability']:

    def wasp(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            wasp
        ).SetName("G.I.R.L.")
        .SetTarget(has_printed_res="B", range=(1, 2), from_where=["YourDiscardPile"])
        .LimitOncePerRound(),
    ]

