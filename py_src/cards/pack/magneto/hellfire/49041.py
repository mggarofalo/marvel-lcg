from . import *

# The Inner Circle

def GetAbilities() -> Sequence['Ability']:

    def the_inner_circle_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Worlds.FindCardSizeOnField(effect, trait="HELLFIRE")
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_inner_circle_revealed
        ),
    ]

