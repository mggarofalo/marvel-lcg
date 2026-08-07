from . import *

# * Giant-Man

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).health >= 3,
            attack=2,
            change_on_event=OnEvent.Health("This")
        )
    ]

