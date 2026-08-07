from . import *

# * S'ym

def GetAbilities() -> Sequence['Ability']:

    def sym_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Ruler of Limbo",
            card_type=SchemeSide2
        )

        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sym_revealed
        ),
    ]

