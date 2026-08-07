from . import *

# Restrained

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Friend,
            highest_atk=True,
            when_attach_exhaust_attached=True
        ),
        *AbilityFactory.CardCannotReady(
            "AttachedCharacter",
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YR")),
    ]

