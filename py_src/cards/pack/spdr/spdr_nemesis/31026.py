from . import *

# Giant Monster Attack

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AdditionalCostToThwartThisScheme(
            None,
            CostFunc.Spend(Cost("Y")),
        ),
    ]

