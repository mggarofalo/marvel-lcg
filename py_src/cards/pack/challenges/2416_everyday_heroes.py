from . import *

# Everyday Heroes

def GetAbilities() -> Sequence['Ability']:

    def everyday_heroes(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Challenge,
            None,
            everyday_heroes,
            to_form=Hero,
        )
    ]

