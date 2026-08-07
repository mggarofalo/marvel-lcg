from . import *

# Captive Hope

def GetAbilities() -> Sequence['Ability']:

    def captive_hope_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            name="Hope Summers",
        )

        Faces.ExhaustAll(faces, effect)


    return [
        *AbilityFactory.CardCannotReady(
            CardFinder(name="Hope Summers"),
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            captive_hope_revealed
        ),
    ]

