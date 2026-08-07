from . import *

# Killer for Hire

def GetAbilities() -> Sequence['Ability']:

    def killer_for_hire_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            killer_for_hire_revealed
        ),
    ]

