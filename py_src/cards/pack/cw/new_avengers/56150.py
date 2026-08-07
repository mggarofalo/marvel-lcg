from . import *

# * Falcon

def GetAbilities() -> Sequence['Ability']:

    def falcon_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.GiveBoostCard(this, effect)

    def falcon_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsConfused():
            this.GainBoostIcons(2, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            falcon_defeated,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            falcon_boost
        ),
    ]

