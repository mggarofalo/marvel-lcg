from . import *

# * Vivian

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Condition.PlayerInMassForm("You", "Intangible", effect),
            thwart=+2,
            change_on_event=OnEvent.Form("YourIdentity")
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Condition.PlayerInMassForm("You", "Dense", effect),
            attack=+2,
            change_on_event=OnEvent.Form("YourIdentity")
        ),
    ]

