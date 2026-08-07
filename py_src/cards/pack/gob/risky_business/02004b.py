from . import *

# Hostile Takeover - 1B

def GetAbilities() -> Sequence['Ability']:

    def hostile_takeover(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        CriminalEnterprisePlaceInfamy("1*", effect)

        counters = CriminalEnterpriseGetInfamyCounters(effect)
        def action(player: 'Player'):
            player.DiscardDeckTopCards(counters, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenMainSchemeStageCompleted(
            AbilityType.WhenCompleted,
            "This",
            hostile_takeover
        )
    ]
