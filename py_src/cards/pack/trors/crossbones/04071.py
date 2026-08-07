from . import *

# Cornered Staff

def GetAbilities() -> Sequence['Ability']:

    def cornered_staff_boost(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.DiscardEncounterCards("1*", effect)
        boost = FacesCounter.CountTotalBoostIcons(faces)
        this.PlaceThreatOnSchemes([this], boost, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cornered_staff_boost
        ),
    ]

