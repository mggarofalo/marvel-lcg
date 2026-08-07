from . import *

# The Power in All of Us

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder(card_class="Basic")
        ),
    ]

