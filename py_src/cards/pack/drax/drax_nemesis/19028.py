from . import *

# Challenge Accepted

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_atk=True
        ),
        *AbilityFactory.AfterUnitDealsDamageWithSingleAttack(
            AbilityType.ForcedResponse,
            CardFinder(name="Drax"),
            "AttachedEnemy",
            DiscardThisCard,
            dealt_more_than_damage=4,
        )
    ]

