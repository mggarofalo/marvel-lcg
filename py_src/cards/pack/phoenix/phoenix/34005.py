from . import *

# * Phoenix Suit

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Phoenix"),
            trait="AERIAL",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            get_new_value=lambda effect, face, ui:
                face.HasTrait("RESTRAINED"),
            steady=1,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity"),
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            get_new_value=lambda effect, face, ui:
                face.HasTrait("UNLEASHED"),
            retaliate=1,
            ex_change_on_event=OnEvent.Trait("AttachedIdentity"),
        ),
    ]

