from . import *

# Laser Goggles

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=-1,
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "YourHero",
            is_basic_attack=True,
            overkill=True,
        )
    ]

