from . import *

# Acute Control

def GetAbilities() -> Sequence['Ability']:

    def acute_control(effect: 'Effect', message: 'Message.AfterIgnoreKeywordOnCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterIgnoreKeywordOnCard(
            AbilityType.HeroResponse,
            "You",
            Minion,
            ["Guard", "Patrol"],
            acute_control,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Targets"),
    ]

