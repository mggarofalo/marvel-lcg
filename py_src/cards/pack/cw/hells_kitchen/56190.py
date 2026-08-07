from . import *

# * Iron Fist

def GetAbilities() -> Sequence['Ability']:

    def iron_fist_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        def action():
            this.DoAttackYou(player, effect)
        this.DoAttackYou("YourLeader", effect, if_no_attack_was_made=action)

    def iron_fist_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.DoAttackYou(player, effect, property=AttackProperty(do_not_give_boost=True))


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            iron_fist_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            iron_fist_boost
        ),
    ]

