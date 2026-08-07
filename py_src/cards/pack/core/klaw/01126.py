from . import *

# Illegal Arms Factory

def GetAbilities() -> Sequence['Ability']:

    def illegal_arms_factory(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], "1*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            illegal_arms_factory
        )
    ]
