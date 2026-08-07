from . import *

# Underground Distribution - 1B

def GetAbilities() -> Sequence['Ability']:

    def underground_distribution(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:

        first_player = Worlds.GetFirstPlayer(effect)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(first_player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            underground_distribution
        )
    ]
