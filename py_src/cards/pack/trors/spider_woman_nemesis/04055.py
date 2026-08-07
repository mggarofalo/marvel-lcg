from . import *

# The Viper's Ambition

def GetAbilities() -> Sequence['Ability']:

    def the_vipers_ambition_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_vipers_ambition_revealed
        ),
    ]

