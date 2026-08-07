from . import *

# Exhaustion

def GetAbilities() -> Sequence['Ability']:

    def exhaustion(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.ExhaustAll([identity], effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            exhaustion
        )
    ]
