from . import *

# We Are One

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Venom Goblin")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR"))
    ]

