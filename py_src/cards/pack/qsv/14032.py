from . import *

# Beat 'Em Up

def GetAbilities() -> Sequence['Ability']:

    def beat_em_up(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            beat_em_up
        ).SetPlay()
        .SetTarget("VillainAndYouEngagedMinion", range="All")
    ]

