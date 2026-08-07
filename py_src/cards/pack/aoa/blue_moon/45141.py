from . import *

# * Oracle

def GetAbilities() -> Sequence['Ability']:

    def oracle_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsConfused():
            ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            oracle_revealed
        ),
    ]

