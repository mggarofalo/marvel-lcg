from . import *

# * Mojo

def GetAbilities() -> Sequence['Ability']:

    def mojo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        faces = Worlds.GetOnFieldFriendlyCharacters(effect)
        Faces.PlaceTokensOn(faces, 2, 'threat', effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mojo_revealed
        ),
        Mojo(4)
    ]

