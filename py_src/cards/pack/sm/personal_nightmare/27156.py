from . import *

# Weakness from Within

def GetAbilities() -> Sequence['Ability']:

    def weakness_from_within_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        value = player.hand_cards.GetSize()
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            weakness_from_within_revealed
        ),
    ]

