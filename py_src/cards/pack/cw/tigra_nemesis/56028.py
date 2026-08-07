from . import *

# * Jigsaw

def GetAbilities() -> Sequence['Ability']:

    def jigsaw(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Criminal Underworld", card_type=EncounterSideScheme)
        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            jigsaw
        ),
    ]

