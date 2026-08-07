from . import *

# * Tweedledope

def GetAbilities() -> Sequence['Ability']:

    def tweedledope_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            player.DiscardControlCards(effect, upgrade=True)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tweedledope_revealed
        ),
    ]

