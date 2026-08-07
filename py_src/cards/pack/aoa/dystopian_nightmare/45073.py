from . import *

# War-Weary

def GetAbilities() -> Sequence['Ability']:

    def war_weary(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            war_weary
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            war_weary
        ),
    ]

