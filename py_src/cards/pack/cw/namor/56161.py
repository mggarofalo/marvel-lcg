from . import *

# * Neptune's Trident

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Namor"),
            otherwise_attach_to="EnemyLeader"
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedCharacter",
            piercing=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YR"))
    ]

