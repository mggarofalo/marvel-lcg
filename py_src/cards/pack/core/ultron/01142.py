from . import *

# Upgraded Drones

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Drone Minion"),
            attack=1,
            health=1,
        ),
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ultron Drones", card_type=Environment)
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR"))
    ]
