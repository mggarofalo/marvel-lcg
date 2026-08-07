from . import *

# * Vision's Cape

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=lambda effect, face, ui:
                Condition.PlayerInMassForm("You", "Dense", effect),
            retaliate=1,
            ex_change_on_event=OnEvent.Form("AttachedIdentity"),
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=lambda effect, face, ui:
                Condition.PlayerInMassForm("You", "Intangible", effect),
            stalwart=1,
            ex_change_on_event=OnEvent.Form("AttachedIdentity"),
        ),
    ]

