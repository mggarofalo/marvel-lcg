from . import *

# Plan B

def GetAbilities() -> Sequence['Ability']:

    def plan_b(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            plan_b
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("RandomHandCard"))
        .SetTarget(Enemy),
    ]

