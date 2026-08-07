from . import *

# The Power of Leadership

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder(card_class="Leadership")
        ),
    ]

