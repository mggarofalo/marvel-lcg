from . import *

# Imminent Overload

def GetAbilities() -> Sequence['Ability']:

    def imminent_overload(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            imminent_overload
        ),
    ]

