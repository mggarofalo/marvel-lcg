from . import *

# * SP//dr

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisCannotBeTreatedAsBlank(),
        Spdr().SetName("Suit Up!"),
        Spdr2(),
    ]

