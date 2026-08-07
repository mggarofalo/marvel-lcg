from . import *

# * Wasp's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder2("GIANT", Hero),
            thwart=1,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity"),
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder2("TINY", Hero),
            attack=1,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity"),
        ),
    ]

