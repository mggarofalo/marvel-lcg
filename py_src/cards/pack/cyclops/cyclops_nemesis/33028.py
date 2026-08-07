from . import *

# * Mister Sinister

def GetAbilities() -> Sequence['Ability']:

    def mister_sinister_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            mister_sinister_boost
        ),
    ]

