from . import *

# Mutated Soldier

def GetAbilities() -> Sequence['Ability']:

    def mutated_soldier_boost(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.HealHealth(this.sustained, effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            mutated_soldier_boost
        ),
    ]

