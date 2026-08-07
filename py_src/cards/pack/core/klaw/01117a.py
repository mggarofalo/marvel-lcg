from . import *

# Secret Rendezvous - 2A

def GetAbilities() -> Sequence['Ability']:

    def secret_rendezvous(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:

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
            secret_rendezvous
        )
    ]
