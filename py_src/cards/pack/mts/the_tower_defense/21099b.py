from . import *

# The Armies of Thanos

def GetAbilities() -> Sequence['Ability']:

    def the_armies_of_thanos(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)
        this.RemoveThreatFromSchemes([this], "All", effect)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.WhenCompleted,
            "This",
            the_armies_of_thanos,
        ),
    ]

