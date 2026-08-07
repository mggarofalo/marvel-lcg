from . import *

# Connection to the Worldmind

def GetAbilities() -> Sequence['Ability']:

    return [
        # Nova’s Connection to the Worldmind is considered an extra card that you can have in your hand. When you draw up to your maximum hand size, you do not count Connection to the Worldmind as one of those cards.
        AbilityFactory.ThisDoesNotCountTowardHandSize()
    ]

