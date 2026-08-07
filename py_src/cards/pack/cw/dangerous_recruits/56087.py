from . import *

# * Venom

def GetAbilities() -> Sequence['Ability']:

    def venom_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        leader = Worlds.GetYourTeamLeader(effect)
        if leader:
            assert False
        else:
            this.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            venom_revealed
        ),
    ]

