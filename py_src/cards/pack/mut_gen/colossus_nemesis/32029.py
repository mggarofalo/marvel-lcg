from . import *

# Slammed

def GetAbilities() -> Sequence['Ability']:

    def slammed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            slammed_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        )
    ]

