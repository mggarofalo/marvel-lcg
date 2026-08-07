from . import *

# * Yon-Rogg

def GetAbilities() -> Sequence['Ability']:

    def yon_rogg(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="The Psyche-Magnitron",
            card_type=SchemeSide2
        )

        if face:
            this.PlaceThreatOnSchemes([face], 1, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            yon_rogg
        )
    ]

