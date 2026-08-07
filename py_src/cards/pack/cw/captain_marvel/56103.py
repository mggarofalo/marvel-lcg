from . import *

# Energy Absorption

def GetAbilities() -> Sequence['Ability']:

    def energy_absorption_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = PlaceEnergyCounterOnEnergyChannel(1, effect)
        villain = FindCaptainMarvel(effect)
        if villain and face:
            value = face.GetCounters('energy')
            this.HealthUnits([villain], value, effect)

    def energy_absorption_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        PlaceEnergyCounterOnEnergyChannel(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            energy_absorption_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            energy_absorption_boost
        ),
    ]

