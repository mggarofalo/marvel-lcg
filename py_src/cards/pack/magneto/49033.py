from . import *

# * Surge: Noriko Ashida

def GetAbilities() -> Sequence['Ability']:
    return [
        *AbilityFactory.ThisNotCountAllyLimit(
            conditions=[
                lambda effect, message:
                    ControlHaveMutantOrXMen(effect) != 0,
            ],
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
        AbilityFactory.ThisGainKeyword(
            ControlHaveMutantOrXMenUI,
            attack=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
    ]

