from . import *

# Dangerous Recruits

def GetAbilities() -> Sequence['Ability']:

    def dangerous_recruits(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        leader = Worlds.GetEnemyLeader(effect)
        if leader:
            leader.GiveBoostCard(message.trigger, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            Minion,
            dangerous_recruits
        ),
    ]

