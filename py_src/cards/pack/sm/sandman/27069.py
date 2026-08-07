from . import *

# Tidal Sands

def GetAbilities() -> Sequence['Ability']:

    def tidal_sands_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        city_streets = GetCityStreets(effect)
        if city_streets:
            value = city_streets.GetCounters('sand')
            this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tidal_sands_revealed
        ),
    ]

