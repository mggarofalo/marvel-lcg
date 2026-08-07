from . import *

# All Tied Up

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        *AbilityFactory.CardCannotReady(
            "AttachedCharacter",
        ),
        AbilityFactory.PlayersCannotChangeForms(
            AbilityType.NonKeyword,
            "AttachedPlayer",
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCost(Cost("BR"))
    ]

