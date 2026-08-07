from . import *

# Kree Lieutenant

def GetAbilities() -> Sequence['Ability']:

    def kree_lieutenant_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GainBoostIcon(3, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            kree_lieutenant_boost,
            during_attack=True,
        ),
    ]

