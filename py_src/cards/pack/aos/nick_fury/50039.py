from . import *

# Spray Fire

def GetAbilities() -> Sequence['Ability']:

    def spray_fire(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect, property=AttackProperty(ranged=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spray_fire
        ).SetPlay().SetLabel('attack')
        .SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

