from . import *

# * Titania

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Minion).health,
            attack=1,
            change_on_event=OnEvent.Health("This")
        ),
    ]

