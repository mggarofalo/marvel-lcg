from . import *

# * Rhino (II)

def GetAbilities() -> Sequence['Ability']:

    def rhino(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Breakin' & Takin'",
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rhino
        )
    ]
