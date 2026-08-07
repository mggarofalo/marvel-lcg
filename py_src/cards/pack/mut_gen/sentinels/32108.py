from . import *

# Relentless Robots

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayersCannotThwartWhile(
            PlayerFinder(engaged_with=CardFinder2("SENTINEL", Minion)),
            "This"
        ),
    ]

