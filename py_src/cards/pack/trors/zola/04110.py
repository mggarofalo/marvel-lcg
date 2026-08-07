from . import *

# Zola (II)

def GetAbilities() -> Sequence['Ability']:

    def zola_ii(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Test Subjects",
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zola_ii
        )
    ]

