from . import *

# Seduced

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.PlayersCannotMakeBasicAttack(
            "You",
        ),
        AbilityFactory.PlayersCannotPlayCardWhile(
            "You",
            CardFinder2("ATTACK", Event),
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("YB")),
    ]

