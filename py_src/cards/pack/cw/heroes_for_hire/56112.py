from . import *

# * Colleen Wing

def GetAbilities() -> Sequence['Ability']:

    def colleen_wing_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.HasTrait("UNREGISTERED"):
            team = Worlds.GetEnemyTeam(effect)
        else:
            team = player

        team.AskDiscardFace(player.GetControlAllies(), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            colleen_wing_revealed
        ),
    ]

