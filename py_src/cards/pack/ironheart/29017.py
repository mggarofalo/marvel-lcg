from . import *

# Go All Out

def GetAbilities() -> Sequence['Ability']:

    def go_all_out(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        damage = hero.thwart + hero.attack + hero.defense
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            go_all_out
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetTarget(Enemy),
    ]

