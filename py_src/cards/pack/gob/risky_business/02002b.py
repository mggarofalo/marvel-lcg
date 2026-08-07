from . import *

# Green Goblin (II)

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealDealDamangeToPlayers(4, False, True),
        RemoveMadnessCounterInsteadOfScheme(1)
    ]

