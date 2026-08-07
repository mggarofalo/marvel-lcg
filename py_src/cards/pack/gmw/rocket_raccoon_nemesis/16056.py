from . import *

# Blackjack's Bazooka

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Blackjack O'Hare"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BBB")),
    ]

