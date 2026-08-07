from . import *

# Bomb Scare

def GetAbilities() -> Sequence['Ability']:

    def bomb_scare(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bomb_scare
        ),
    ]
