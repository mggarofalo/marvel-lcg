from . import *

# * Manta

def GetAbilities() -> Sequence['Ability']:

    def manta_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            manta_revealed
        ),
    ]

