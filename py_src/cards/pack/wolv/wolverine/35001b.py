from . import *

# Logan

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.SetupPutIntoPlay(
            ["35002"],
        )
    ]

