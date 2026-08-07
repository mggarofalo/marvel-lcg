from . import *

# Battle Fury

def GetAbilities() -> Sequence['Ability']:

    def battle_fury(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "YourHero",
            Minion,
            battle_fury,
        ).SetTarget("YourHero", canbe_ready=True)
        .SetCostFunc(CostFunc.DealDamage(1, "YourHero"))
        .SetCostFunc(CostFunc.Discard("This"))
    ]

