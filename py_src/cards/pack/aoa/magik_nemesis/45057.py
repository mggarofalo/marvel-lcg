from . import *

# * Witchfire

def GetAbilities() -> Sequence['Ability']:

    def witchfire(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Ruler of Limbo",
            card_type=SchemeSide2
        )
        if face:
            this.PlaceThreatOnSchemes([face], 1, effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedInterrupt,
            "This",
            Ally,
            witchfire
        ),
    ]

