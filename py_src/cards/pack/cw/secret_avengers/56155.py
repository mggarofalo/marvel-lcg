from . import *

# * Black Panther

def GetAbilities() -> Sequence['Ability']:

    def black_panther_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.GiveBoostCard(this, effect)

    def black_panther_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlUpgrade()
        Faces.ExhaustAll(faces, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            black_panther_defeated,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            black_panther_boost
        ),
    ]

