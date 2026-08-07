from . import *

# Impossible Geometry

def GetAbilities() -> Sequence['Ability']:

    def impossible_geometry_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsConfused():
            player.DiscardControlCards(effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            impossible_geometry_revealed
        ),
    ]

