from . import *

# Earth's Mightiest Heroes

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.EachGainPrintedKeywordsOfEachOther(
            CardFinder2("AVENGER", Minion),
            CardFinder2("AVENGER", Minion),
        ),
    ]

