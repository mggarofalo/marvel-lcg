from . import *

# The Anachronauts

def GetAbilities() -> Sequence['Ability']:

    def the_anachronauts(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPileCards(effect)
        faces = CardFinder2("TEMPORAL").Checks(faces)
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_anachronauts,
        ),
    ]

