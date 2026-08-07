from . import *

# The Psyche-Magnitron

def GetAbilities() -> Sequence['Ability']:

    def the_psyche_magnitron(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_psyche_magnitron
        )
    ]

