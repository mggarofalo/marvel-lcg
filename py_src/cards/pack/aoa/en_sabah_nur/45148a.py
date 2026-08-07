from . import *

# The Rise of Apocalypse

def GetAbilities() -> Sequence['Ability']:

    def the_rise_of_apocalypse_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            trait="SUPERPOWER",
        )
        if face:
            face.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_rise_of_apocalypse_revealed
        ),
    ]

