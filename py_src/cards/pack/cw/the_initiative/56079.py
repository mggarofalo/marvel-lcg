from . import *

# * Hellcat

def GetAbilities() -> Sequence['Ability']:

    def hellcat_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            identity.TakeIndirectDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)

    def hellcat_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hellcat_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hellcat_boost
        ),
    ]

