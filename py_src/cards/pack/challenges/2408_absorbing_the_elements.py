from . import *

# Absorbing the Elements

def GetAbilities() -> Sequence['Ability']:

    def absorbing_the_elements(effect: 'Effect', message: 'Message.WhenGameOver'):
        for name in ["Groot", "Iceman", "Rockslide", "Colossus"]:
            if not Worlds.FindCardOnField(
                effect,
                name=name,
                card_type=Ally|Hero
            ):
                return False

    return [
        AbilityFactory.WhenScenarioEnd(
            absorbing_the_elements
        )
    ]

