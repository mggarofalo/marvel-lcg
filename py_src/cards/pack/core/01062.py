from . import *

# The Power of Justice

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder(card_class="Justice")
        ),
    ]

