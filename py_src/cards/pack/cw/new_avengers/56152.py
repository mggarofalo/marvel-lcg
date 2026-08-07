from . import *

# * Goliath

def GetAbilities() -> Sequence['Ability']:

    def goliath_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.GiveBoostCard(this, effect)

    def goliath_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            if leader.IsTough():
                this.GainBoostIcons(2, effect)
            else:
                Faces.GiveStatus([leader], "Tough", effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            goliath_defeated,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            goliath_boost
        ),
    ]

