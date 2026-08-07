from . import *

# * Jester

def GetAbilities() -> Sequence['Ability']:

    def jester_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsConfused():
            player.DiscardControlCards(effect, support=True)
        else:
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            jester_revealed
        ),
    ]

