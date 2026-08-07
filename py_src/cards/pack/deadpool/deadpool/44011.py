from . import *

# It Ain't Over...

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(MainScheme),
        *AbilityFactory.GiveKeywordToAttached(
            MainScheme,
            get_new_value=lambda effect, attach, ui:
                attach.acceleration_token,
            target_threat=2,
            ex_change_on_event=OnEvent.AccelerationToken("AttachedScheme")
        )
    ]

