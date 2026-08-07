from . import *

# * Kidpool: Wade "Tito" Wilson

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        )
    ]

