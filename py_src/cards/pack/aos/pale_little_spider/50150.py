from . import *

# Pride of the Red Room

def GetAbilities() -> Sequence['Ability']:

    def pride_of_the_red_room_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Worlds.FindCardSizeOnField(effect, trait="PREPARATION")

        this.PlaceThreatOnSchemes([this], value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pride_of_the_red_room_revealed
        ),
    ]

