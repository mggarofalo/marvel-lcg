from . import *

# Pain Inhibitors

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_remaining_hp=True,
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=2,
            retaliate=1,
        ),
    ]

