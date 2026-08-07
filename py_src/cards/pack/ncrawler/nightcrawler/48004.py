from . import *

# * Kurt's Cutlasses

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Nightcrawler"),
            attack=1,
            defense=1,
            retaliate=1,
        ),
    ]

