from . import *

# The Great Hunt

def GetAbilities() -> Sequence['Ability']:

    def the_great_hunt_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        value = 0
        for identity in units:
            size = identity.GetPlacedCardArea().FindCardSize()
            value += size

        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_great_hunt_revealed
        ),
    ]

