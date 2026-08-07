from . import *

# Surge in Crime

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("CRIMINAL", Minion),
            surge=1
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(effect, trait="CRIMINAL", card_type=Minion) == None
            ]
        ).SetCost(Cost("2")),
    ]

