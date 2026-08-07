from . import *

# Psionic Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Adaptoid"),
            villainous=1,
            trait="PSIONIC"
        ),
    ]

