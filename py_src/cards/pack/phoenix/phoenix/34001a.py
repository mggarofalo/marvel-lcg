from . import *

# * Phoenix

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G")
        ).SetName("Psionic Bond")
        .SetCostFunc(CostFunc.Counter(CardFinder(name="Phoenix Force"), 1, 'power'))
        .LimitOncePerPhase(),
    ]

