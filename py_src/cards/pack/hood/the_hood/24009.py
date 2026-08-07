from . import *

# The Hood's Pistol

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="The Hood")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BR")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        )
    ]

