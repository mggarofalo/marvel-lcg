from . import *

# Goblin Glider

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_printed_hp=True,
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YY")),
    ]

