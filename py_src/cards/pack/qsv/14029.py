from . import *

# Brute Force

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "You",
            is_basic_attack=True,
            piercing=True
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "You",
            DiscardThisCard,
            is_basic_attack=True
        )
    ]

