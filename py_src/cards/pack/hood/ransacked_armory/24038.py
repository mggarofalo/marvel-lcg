from . import *

# Holoshield Generator

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_remaining_hp=True,
            if_cannot_operation=SearchEncounterDeckForMinionAndAttachTo,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=4,
            retaliate=2,
        ),
    ]

