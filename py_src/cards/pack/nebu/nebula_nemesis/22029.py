from . import *

# Self-Preservation

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Nebula"),
            thwart=-1,
            attack=-1,
            defense=-1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Gamora"),
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Gamora"),
            piercing=True
        ),
    ]

