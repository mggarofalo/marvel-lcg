from . import *

# "The War's Been Brought"

def GetAbilities() -> Sequence['Ability']:

    def the_wars_been_brought_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        value = 0
        value += player.hand_cards.FindCardSize(CardFinder(has_printed_res="G"))
        value += Faces.FindCardSize(player.GetControlCards(), CardFinder(has_printed_res="G"))
        value += player.discard_pile.FindCardSize(CardFinder(has_printed_res="G"))

        Worlds.DiscardEncounterCards(
            value,
            effect
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_wars_been_brought_revealed
        ),
    ]

