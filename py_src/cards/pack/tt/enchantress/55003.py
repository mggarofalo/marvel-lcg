from . import *

# * Enchantress

def GetAbilities() -> Sequence['Ability']:

    def enchantress_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = SetupCards.PutIntoPlay(
            effect,
            name="Future of Despair",
            from_where=["SetAside"],
        )

        if face:
            this.PlaceThreatOnSchemes([face], "3*", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            enchantress_revealed
        ),
    ]

