from . import *

# Excessive Force

def GetAbilities() -> Sequence['Ability']:

    def excessive_force_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        size = len(Worlds.GetOnFieldMinions(effect))
        if size:
            this.PlaceThreatOnSchemes("MainScheme", size, effect)
        else:
            this.GainSurge(+1, effect)

    def excessive_force_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        # player = message.GetToPlayer()

        # leader = Worlds.GetEnemyTeamLeader(effect)
        # Worlds.GetYourTeamLeader()
        # TODO: your leader
        # leader.DoAttackYou(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            excessive_force_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            excessive_force_boost
        ),
    ]

