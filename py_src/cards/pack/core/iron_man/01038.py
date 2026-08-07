from . import *

# Powered Gauntlets

def GetAbilities() -> Sequence['Ability']:

    def powered_gauntlets(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        damage = 1
        if hero.HasTrait("AERIAL"):
            damage = 2
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            powered_gauntlets
        ).SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]

