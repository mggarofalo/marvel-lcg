from . import *

# * SP//dr Suit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisCannotBeTreatedAsBlank(),
        Spdr().SetName("Return to Base"),
        Spdr2(),
    ]

