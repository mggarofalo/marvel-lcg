from . import *

# Cloak and Dagger

def GetAbilities() -> Sequence['Ability']:

    def cloak_and_dagger_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.HealthUnits("EnemyLeader", 3, effect)
        Faces.GiveStatus("EnemyLeader", "Tough", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            cloak_and_dagger_defeated,
        ),
    ]

