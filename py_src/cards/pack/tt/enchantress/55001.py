from . import *

# * Enchantress

def GetAbilities() -> Sequence['Ability']:

    return [
        AfterEnchantressAttacksYouPlaceCharmCounter()
    ]

