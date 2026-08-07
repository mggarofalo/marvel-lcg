from . import *

# The Fifty State Initiative

def GetAbilities() -> Sequence['Ability']:

    def the_fifty_state_initiative_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPileCards(effect, CardFinder2("AVENGER", Minion))
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_fifty_state_initiative_defeated,
        ),
    ]

