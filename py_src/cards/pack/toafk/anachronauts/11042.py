from . import *

# * Sir Raston

def GetAbilities() -> Sequence['Ability']:

    def sir_raston_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.TakeDamage(this, 1, effect)
        message.GiveBoostCardForThisActivation(Enemy, 1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sir_raston_boost
        ),
    ]

