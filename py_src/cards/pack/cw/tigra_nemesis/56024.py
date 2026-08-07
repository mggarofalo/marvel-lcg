from . import *

# * The Hood

def GetAbilities() -> Sequence['Ability']:

    def the_hood(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Criminal Underworld", card_type=EncounterSideScheme)
        if face:
            this.PlaceThreatOnSchemes([face], 1, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            the_hood
        ),
    ]

