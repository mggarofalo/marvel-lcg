from . import *

# Anti-Hero Propaganda

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Hero,
            thwart=-1,
            attack=-1,
            defense=-1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("G"))
        .SetCostFunc(CostFunc.TakeDamage(2, "YourIdentity")),
    ]

