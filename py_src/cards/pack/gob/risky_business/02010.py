from . import *

# Oscorp Manufacturing

def GetAbilities() -> Sequence['Ability']:

    def oscorp_manufacturing(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            VillainIsNormanOsborn,
            oscorp_manufacturing
        )
    ]

