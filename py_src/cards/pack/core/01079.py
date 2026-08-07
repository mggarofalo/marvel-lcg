from . import *

# The Power of Protection

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder(card_class="Protection")
        ),
    ]

