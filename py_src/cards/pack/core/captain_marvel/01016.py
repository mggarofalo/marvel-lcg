from . import *

# * Captain Marvel's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain Marvel"),
            defense=1,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain Marvel", trait="AERIAL"),
            defense=1,
            ex_change_on_event=OnEvent.Trait(CardFinder(name="Captain Marvel"))
        )
    ]

