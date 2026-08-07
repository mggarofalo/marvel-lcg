from . import *

# * Ronin

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                len(effect.this.CastTo(Ally).GetAttachedUpgrades()) > 0,
            attack=1,
            thwart=1,
            change_on_event=OnEvent.AttachedMove(Upgrade)
        )
    ]

