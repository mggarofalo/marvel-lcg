from . import *

# Whatever It Takes

def GetAbilities() -> Sequence['Ability']:

    def whatever_it_takes_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        your_leader = Worlds.GetYourTeamLeader(effect)
        if your_leader and leader:
            leader.BasicAttack([your_leader], effect)

    def whatever_it_takes_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            whatever_it_takes_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            whatever_it_takes_hero
        ),
    ]

