from . import *

# Flamethrower

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_remaining_hp=True,
            if_cannot_operation=SearchEncounterDeckForMinionAndAttachTo
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedMinion",
            indirect_damage=True,
        ),
    ]

