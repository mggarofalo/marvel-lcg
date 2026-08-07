from . import *

# Laser Goggles

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=-1,
            attack=2,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "YourHero",
            is_basic_attack=True,
            overkill=True,
            piercing=True
        )
    ]

