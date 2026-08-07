from . import *

# Kree Private

def GetAbilities() -> Sequence['Ability']:

    def kree_private_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            kree_private_boost,
            during_attack=True,
        ),
    ]

