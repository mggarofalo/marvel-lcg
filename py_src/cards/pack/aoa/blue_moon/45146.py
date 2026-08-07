from . import *

# Trial by Combat

def GetAbilities() -> Sequence['Ability']:

    def trial_by_combat(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPile(effect).FindCards(
            trait="IMPERIAL GUARD",
            card_type=Minion
        )

        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            trial_by_combat,
        ),
    ]

