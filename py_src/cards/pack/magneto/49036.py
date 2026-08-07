from . import *

# * Indra: Paras Gavaskar

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
            health=2,
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
    ]

