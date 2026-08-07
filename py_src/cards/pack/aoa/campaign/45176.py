from . import *

# Desperate Measures

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Ally),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            thwart=1,
            attack=1,
            health=1,
            have_res_icon="G",
        ),
    ]

