from . import *

# Kree Commando

def GetAbilities() -> Sequence['Ability']:

    def kree_commando_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.would_atk_message.GainPiercing(effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            kree_commando_boost,
            during_attack=True,
        ),
    ]

