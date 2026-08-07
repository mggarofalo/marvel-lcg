from . import *

# * Aragorn

def GetAbilities() -> Sequence['Ability']:

    return [
        # This card should say “you” instead of “Valkyrie” to work as intended (like how Thor’s Helmet is worded). Go ahead and treat this card as if it said “you” instead. Switching to Alter-Ego would not change the bonuses the card is granting you.
        *AbilityFactory.GiveKeywordToAttached(
            # name="Valkyrie",
            "You",
            health=4,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Valkyrie"),
            trait="AERIAL",
        )
    ]

