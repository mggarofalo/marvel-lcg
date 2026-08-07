from . import *

# * Hercules

def GetAbilities() -> Sequence['Ability']:

    def hercules_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.GiveBoostCard(this, effect)

    def hercules_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            this.GainBoostIcons(2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hercules_defeated,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hercules_boost
        ),
    ]

