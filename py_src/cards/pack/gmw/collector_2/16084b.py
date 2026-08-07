from . import *

# The Great Escape

def GetAbilities() -> Sequence['Ability']:

    return [
        ExhaustMilanoRemoveThreatFromThisScheme(3),
        AbilityFactory.IfNoThreatHerePlayersWinTheGame(),
    ]

