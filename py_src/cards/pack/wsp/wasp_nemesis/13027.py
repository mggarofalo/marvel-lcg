from . import *

# Mother's Orders

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AdditionalCostToAttack(
            Hero,
            CostFunc.Spend(Cost("1")),
            is_basic_attack=True,
        ),
    ]

