from . import *

# Laser Blaster

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("GUARDIAN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedAlly",
            overkill=True
        )
    ]

