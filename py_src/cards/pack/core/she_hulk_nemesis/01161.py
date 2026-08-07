from . import *

# Personal Challenge

def GetAbilities() -> Sequence['Ability']:

    def personal_challenge_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            personal_challenge_revealed
        ),
    ]

