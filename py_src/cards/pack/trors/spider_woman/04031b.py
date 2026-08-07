from . import *

# * Jessica Drew

def GetAbilities() -> Sequence['Ability']:

    def jessica_drew(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        for face in effect.targets:
            initiator.LookAtDeck(face.card.area, 1, effect)

    return [
        # Double Agent — Choose two aspects instead of one during deck-building. You must include an equal number of cards from those aspects in your deck.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            jessica_drew
        ).SetTargetInternal(Select.From(not_shuffle=True, not_move=True, from_where=["AnyDecksTopCard"]))
        .LimitOncePerRound(),
    ]

