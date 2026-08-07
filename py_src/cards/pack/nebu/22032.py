from . import *

# Energy Spear

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("GUARDIAN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            attack=2,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedAlly",
            piercing=True
        ),
    ]

