from . import *

# Justice Like Lightning

def GetAbilities() -> Sequence['Ability']:

    def justice_like_lightning_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            leader = Worlds.GetEnemyLeader(effect)
            if leader:
                leader.DoActivate(player, effect, do_not_give_boost=True)

        Worlds.Enemies.AllMinionActivateAgainstEngagedPlayer(effect, if_no_activate=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            justice_like_lightning_revealed
        ),
    ]

