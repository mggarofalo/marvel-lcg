from . import *

# Kree Combat Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_atk=True
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            Unit2,
            1,
            this_attached_unit_only=True,
            from_each_attack=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("3", same_type=True)),
    ]

