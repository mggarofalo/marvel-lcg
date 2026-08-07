from . import *

# Serpent Soldier

def GetAbilities() -> Sequence['Ability']:

    def serpent_soldier_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveActivatingEnemyAdditionalBoostCard(1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            serpent_soldier_boost
        ),
    ]

