from . import *
from cards.pack.trors.campaign import *

# None Shall Pass

def GetAbilities() -> Sequence['Ability']:

    def none_shall_pass_revealed(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Environment,
        )
        assert face
        face.PutIntoPlay(first_player, effect)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        encounter_deck.ShuffleWithDiscardPile(False, effect)

    return [
        *CampaignSetup(2),
        AbilityFactory.WhenCardSetup(
            "This",
            none_shall_pass_revealed
        ),
    ]

