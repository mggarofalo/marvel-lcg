from . import *

# The One with the Breakup

def GetAbilities() -> Sequence['Ability']:

    def the_one_with_the_breakup(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:

        for target in effect.targets:
            target.GetControlByPlayer().DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            EncounterCard,
            peril=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
            ex_operation=the_one_with_the_breakup
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            range=(3, 3)))
        .SetTarget("Players")
    ]

