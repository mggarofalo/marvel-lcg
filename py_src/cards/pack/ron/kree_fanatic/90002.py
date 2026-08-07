from . import *

# Judge, Jury, Executioner

def GetAbilities() -> Sequence['Ability']:

    def judge_jury_executioner(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            Friend,
            judge_jury_executioner,
            by_attack=True,
            attacker=Enemy
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

