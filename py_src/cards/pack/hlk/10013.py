from . import *

# * She-Hulk

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).sustained,
            attack=1,
            change_on_event=OnEvent.Health("This")
        )
    ]

