from . import *

# * Leader of the Guardians

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("GUARDIAN", Unit2),
            control_by="You",
            thwart=1,
            change_on_event=OnEvent.Trait("YouControlCharacter")
        )
    ]

