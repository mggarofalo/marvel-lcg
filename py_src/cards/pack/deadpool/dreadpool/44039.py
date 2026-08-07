from . import *

# Dreadful Deeds

def GetAbilities() -> Sequence['Ability']:

    def dreadful_deeds_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = len([player for player in Worlds.GetPlayers(effect) if player.IsControl(CardFinder(card_class="'Pool"))])
        this.PlaceThreatOnSchemes([this], 2 * value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dreadful_deeds_revealed
        ),
    ]

