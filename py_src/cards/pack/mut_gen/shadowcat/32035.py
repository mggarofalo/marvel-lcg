from . import *

# Intangible Interference

def GetAbilities() -> Sequence['Ability']:

    def intangible_interference(effect: 'Effect', message: 'Message.AfterIgnoreKeywordOnCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterIgnoreKeywordOnCard(
            AbilityType.HeroResponse,
            "You",
            Scheme2,
            ["Crisis"],
            intangible_interference,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Targets"),
    ]

