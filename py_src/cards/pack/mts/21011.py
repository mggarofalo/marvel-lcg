from . import *

# * Captain America: Steve Rogers

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            each_card_you_control=CardFinder2("AVENGER", Unit2)
        ),
    ]

