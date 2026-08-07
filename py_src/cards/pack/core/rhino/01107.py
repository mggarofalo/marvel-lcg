from . import *

# Breakin' & Takin'

def GetAbilities() -> Sequence['Ability']:

    def breakin_and_takin(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            breakin_and_takin
        )
    ]

