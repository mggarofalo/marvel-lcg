from . import *

# Agent of Thanos

def GetAbilities() -> Sequence['Ability']:

    def agent_of_thanos_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        value = player.area_environment.FindCardSize(CardFinder2("SPELL", Environment))
        if not this.PlaceThreatOnSchemes("MainScheme", value, effect):
            ThisCardGainSurge(effect)

    def agent_of_thanos_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        hero = player.GetHero()
        value = player.area_environment.FindCardSize(CardFinder2("SPELL", Environment))
        if not this.DealDamage([hero], value, effect):
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            agent_of_thanos_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            agent_of_thanos_hero
        ),
    ]

