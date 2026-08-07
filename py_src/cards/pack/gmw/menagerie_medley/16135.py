from . import *

# Psionic Ghost

def GetAbilities() -> Sequence['Ability']:

    def psionic_ghost_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsConfused():
            identity.TakeDamage(this, 1, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)


    def psionic_ghost_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            psionic_ghost_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psionic_ghost_boost
        ),
    ]

