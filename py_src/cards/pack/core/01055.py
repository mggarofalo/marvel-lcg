from . import *

# The Power of Aggression

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder(card_class="Aggression")
        ),
    ]

