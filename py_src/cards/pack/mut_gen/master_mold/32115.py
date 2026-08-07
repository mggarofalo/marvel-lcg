from . import *

# Unit Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("SENTINEL", Minion),
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=2,
            retaliate=1,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder2("SENTINEL", Minion)
        ),
    ]

