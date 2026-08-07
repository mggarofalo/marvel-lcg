from . import *

# Green Goblin (III)

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealDealDamangeToPlayers(4, False, False),
        RemoveMadnessCounterInsteadOfScheme(2)
    ]

