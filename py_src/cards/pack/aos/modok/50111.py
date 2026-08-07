from . import *

# Sarah Garza Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Adaptoid"),
            trait="ELITE",
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Adaptoid"),
            overkill=True,
            ranged=True,
        )
    ]

