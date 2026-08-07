from . import *

# * Wonder Man: Simon Williams

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AdditionalCostToAttack(
            "This",
            CostFunc.Discard("YourHandCards"),
        )
    ]

