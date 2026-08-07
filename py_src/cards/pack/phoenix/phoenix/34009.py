from . import *

# Mind Control

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            NonEliteMinion,
        ),
        AbilityFactory.TakeControlOfAttachedMinion("phoenix_controlled_ally"),
    ]

