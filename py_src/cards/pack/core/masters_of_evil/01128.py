from . import *

# The Masters of Evil

def GetAbilities() -> Sequence['Ability']:

    def the_masters_of_evil_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="MASTERS OF EVIL"
        )
        if face:
            face.PutIntoPlay(first_player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_masters_of_evil_revealed
        ),
    ]
