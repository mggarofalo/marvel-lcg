from . import *

# Genetic Manipulation

def GetAbilities() -> Sequence['Ability']:

    def genetic_manipulation(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Gene Therapy",
            card_type=Attachment
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            genetic_manipulation,
        ),
    ]

