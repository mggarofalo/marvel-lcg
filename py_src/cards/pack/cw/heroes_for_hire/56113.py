from . import *

# * Shang-Chi

def GetAbilities() -> Sequence['Ability']:

    def shang_chi_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.HasTrait("UNREGISTERED"):
            team = Worlds.GetEnemyTeam(effect)
        else:
            team = player

        team.AskDiscardFace(player.GetControlCardsByType(upgrade=True), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shang_chi_revealed
        ),
    ]

