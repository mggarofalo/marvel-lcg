from . import *

# "Now I'm Mad"

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=lambda effect, attach, ui:
                attach.health < attach.starting_health/2,
            attack=+1,
            thwart=-1,
            ex_change_on_event=OnEvent.Health("AttachedIdentity"),
        ),
    ]

