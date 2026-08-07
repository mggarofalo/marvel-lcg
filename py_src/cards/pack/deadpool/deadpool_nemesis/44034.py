from . import *

# Involuntary Procedures

def GetAbilities() -> Sequence['Ability']:

    def involuntary_procedures(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], 1, effect)
        if this.threat >= 10:
            Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Deadpool"),
            involuntary_procedures,
        ),
    ]

