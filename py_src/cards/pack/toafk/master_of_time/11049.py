from . import *

# Fear of Kang

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayersCannotAttackWhile(
            "You",
            CardFinder(name="Kang")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard("RandomHandCard")),
    ]

