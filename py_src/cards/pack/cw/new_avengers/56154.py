from . import *

# New Avengers

def GetAbilities() -> Sequence['Ability']:

    def new_avengers_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPileCards(effect, CardFinder(card_type=Minion))
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            new_avengers_defeated,
        ),
    ]

