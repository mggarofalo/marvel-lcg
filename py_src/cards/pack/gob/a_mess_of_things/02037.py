from . import *

# A Mess of Things

def GetAbilities() -> Sequence['Ability']:

    def a_mess_of_things_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetOnFieldFriendlyCharacters(effect)
        size = Faces.FindCardSize(faces, CardFinder(is_stunned=True))
        this.PlaceThreatOnSchemes([this], 2*size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            a_mess_of_things_revealed
        ),
    ]

