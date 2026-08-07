from . import *

# * Earthquake

def GetAbilities() -> Sequence['Ability']:

    def earthquake_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsExhaust():
            ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)
        else:
            Faces.ExhaustAll([identity], effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            earthquake_revealed
        ),
    ]

