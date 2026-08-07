from . import *

# Planetary Invasion

def GetAbilities() -> Sequence['Ability']:

    def planetary_invasion_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if minion:
            minion.Reveal(player, effect)
            Faces.GiveStatus([minion], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            planetary_invasion_revealed
        ),
    ]

