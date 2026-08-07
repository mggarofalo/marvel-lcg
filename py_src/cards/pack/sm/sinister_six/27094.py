from . import *

# * Doctor Octopus

def GetAbilities() -> Sequence['Ability']:

    def doctor_octopus(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 1, effect)
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            doctor_octopus,
        ),
        SinisterSixWhenDefeated(),
    ]

