from . import *

# Enhanced Ivory Horn

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Rhino")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("RRR"))
    ]

