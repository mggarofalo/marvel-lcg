from . import *

# Cat in a Tree

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AdditionalCostToThwartThisScheme(
            None,
            CostFunc.TakeIndirectDamage(2)
        ),
    ]

