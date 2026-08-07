from . import *

# Green Goblin (I)

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealDealDamangeToPlayers(3, True, True),
        RemoveMadnessCounterInsteadOfScheme(1)
    ]

