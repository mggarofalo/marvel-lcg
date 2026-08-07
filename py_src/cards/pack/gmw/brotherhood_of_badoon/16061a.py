from . import *

# Terrestrial Invasion

def GetAbilities() -> Sequence['Ability']:

    def terrestrial_invasion(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="Badoon Ship",
            card_type=Environment
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            terrestrial_invasion
        ),
    ]

