from . import *

# Adamantium Upgrades

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            piercing=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
    ]

