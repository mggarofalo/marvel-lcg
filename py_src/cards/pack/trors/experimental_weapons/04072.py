from . import *

# Laser Rifle

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedVillain",
            lambda effect, message:
                message.GainRanged(effect)
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YR")),
    ]

