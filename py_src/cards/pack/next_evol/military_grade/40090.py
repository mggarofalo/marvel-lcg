from . import *

# Heavy Armament

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_atk=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            retaliate=2,
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.HeroResponse,
            "You",
            "AttachedEnemy",
            DiscardThisCard
        ).SetCost(Cost("2", same_type=True)),
    ]

