from . import *

# Frozen

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("YR")),
        *AbilityFactory.CardCannotReady(
            "AttachedIdentity"
        ),
    ]

