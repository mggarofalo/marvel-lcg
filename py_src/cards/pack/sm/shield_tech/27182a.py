from . import *

# Compact Darts

def GetAbilities() -> Sequence['Ability']:

    def compact_darts(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    def compact_darts_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.PlaceCountersOn([this], 3, 'dart', effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            "YourHero",
            compact_darts
        ).SetCostFunc(CostFunc.Counter("This", 1, 'dart'))
        .SetTarget(Enemy),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            compact_darts_action
        ).SetCost(Cost("1"))
        .LimitOncePerRound(),
    ]

