from . import *

# Magical Enhancements

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
            attack=1,
            defense=1,
        ),
        AbilityFactory.DiscardThisWhenRoundEnd(
            AbilityType.ForcedInterrupt,
        )
    ]

