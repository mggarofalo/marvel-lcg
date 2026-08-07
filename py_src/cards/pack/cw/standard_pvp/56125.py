from . import *

# Righteous Cause

def GetAbilities() -> Sequence['Ability']:

    def righteous_cause_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        leader = Worlds.GetEnemyLeader(effect)
        give_additional_boost = 1 if Worlds.GetYourTeamSize(effect) > 1 else 0
        if leader:
            leader.DoSchemes(player, effect, property=SchemeProperty(give_additional_boost=give_additional_boost))


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            righteous_cause_revealed
        ),
    ]

