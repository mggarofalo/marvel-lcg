from . import *

# * Bullseye

def GetAbilities() -> Sequence['Ability']:

    def bullseye_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        leader = Worlds.GetYourTeamLeader(effect)
        if leader:
            # team = Worlds.GetEnemyTeam(effect)
            assert False
        else:
            player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bullseye_revealed
        ),
    ]

