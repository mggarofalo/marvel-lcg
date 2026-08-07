from . import *

# Dauntless

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=lambda effect, attach, ui:
                attach.health >= attach.printed_health,
            retaliate=1,
            ex_change_on_event=OnEvent.Health("AttachedHero")
        ),
    ]

