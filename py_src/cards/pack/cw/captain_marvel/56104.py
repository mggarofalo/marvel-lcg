from . import *

# Alpha Flight Station

def GetAbilities() -> Sequence['Ability']:

    def alpha_flight_station_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        PlaceEnergyCounterOnEnergyChannel(3, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            alpha_flight_station_defeated,
        ),
    ]

